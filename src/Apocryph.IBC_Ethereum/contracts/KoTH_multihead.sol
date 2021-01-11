// SPDX-License-Identifier: MIT
pragma solidity >=0.7.0 <0.9.0;

contract KoTH_multihead {
  struct BlockState {
    bytes32 previous_block;
    bytes32 state_hash;
    uint cumulative_difficulty;
  }

  uint immutable slots_count;
  uint immutable changed_slots_count;
  bytes32 public best_block;
  mapping(bytes32 => BlockState) public blocks;

  constructor (
    bytes32 _root_block,
    bytes32 _root_hash,
    uint _slots_count,
    uint _changed_slots_count
  ) {
    blocks[_root_block].previous_block = bytes32(type(uint).max); // max so that we can use previous_block == 0 to check if a block is new
    blocks[_root_block].cumulative_difficulty = 0;
    blocks[_root_block].state_hash = _root_hash;
    slots_count = _slots_count;
    changed_slots_count = _changed_slots_count;
  }

  function computeStateHash(
    bytes32[] memory heights, // in
    bytes32[] memory seeds, // in
    uint total_difficulty
  ) public view returns (bytes32 hash) {
    require(heights.length == slots_count && seeds.length == slots_count,
      "Wrong state size");
    return keccak256(abi.encodePacked(heights, seeds, total_difficulty));
  }

  function computeEstimatedDifficulty(
    bytes32 height
  ) public pure returns (uint difficulty) {
    return (uint(height) / (type(uint).max - uint(height))) >> 4; // >> 4 so that the difficulty values do not overflow as easily
  }

  function validateSignatures(
    bytes32 signed_data,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds, // in
    uint[1] memory total_difficulty, // inout
    uint8[] calldata signatures_v,
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_s
  ) internal view {
    uint previous_slot = 0;
    for (uint signature = 0; signature < signatures_v.length; signature ++) {
      address signer = ecrecover(signed_data, signatures_v[signature], signatures_r[signature], signatures_s[signature]);

      uint slot = uint(uint160(signer)) % slots_count;

      require(
        signature == 0 || slot > previous_slot, // signature == 0 check is due to previous_slot being uninitialized on the first iteration
        "Signatures must be given in slot order"); // Check is needed so that a slot cannot be used more than once

      previous_slot = slot;
      { // Stack too deep otherwise
        bytes32 height = keccak256(abi.encodePacked(signer, seeds[slot]));

        require(
          uint256(height) >= uint256(heights[slot]),
          "Insufficient signature height");

        if (heights[slot] != height) {
          total_difficulty[0] = total_difficulty[0] - computeEstimatedDifficulty(heights[slot]) + computeEstimatedDifficulty(height);
          heights[slot] = height;
        }
      }
    }
  }

  function rotateSeeds(
    bytes32 new_block,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds, // inout
    uint[1] memory total_difficulty // inout
  ) internal view {
    bytes32 random_walk_value = new_block;
    for (uint i = 0; i < changed_slots_count; i ++) {
      uint slot = uint256(random_walk_value) % slots_count;
      random_walk_value = keccak256(abi.encodePacked(random_walk_value));
      seeds[slot] = random_walk_value;
      total_difficulty[0] -= computeEstimatedDifficulty(heights[slot]);
      heights[slot] = bytes32(0);
    }
  }

  function getConfirmedBlockDiffBrute(
    uint confirmation_difficulty
  ) external view returns (
    bytes32 confirmed_block
  ) {
    confirmed_block = best_block;
    uint wanted_cumulative_difficulty = blocks[confirmed_block].cumulative_difficulty - confirmation_difficulty;
    while (blocks[confirmed_block].cumulative_difficulty > wanted_cumulative_difficulty) {
      confirmed_block = blocks[confirmed_block].previous_block;
    }
  }

  function addBlock(
    bytes32 previous_block,
    bytes32 new_block,
    bytes32[] memory heights,
    bytes32[] memory seeds,
    uint total_difficulty,
    uint8[] calldata signatures_v,
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_s
  ) external {
    require(blocks[new_block].previous_block == bytes32(0),
      "Block already stored");

    require(blocks[previous_block].state_hash == computeStateHash(heights, seeds, total_difficulty),
      "Invalid state");

    require(signatures_v.length == signatures_r.length && signatures_v.length == signatures_s.length,
      "Signatures length mismatch");
    require(signatures_v.length * 3 >= slots_count * 2,
      "Insufficient signature count");

    bytes32 signed_data = keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n64", previous_block, new_block));

    uint[1] memory total_difficulty_ref = [total_difficulty]; // Stack too deep error if using return values...

    validateSignatures(signed_data, heights, seeds, total_difficulty_ref, signatures_v, signatures_r, signatures_s);
    rotateSeeds(new_block, heights, seeds, total_difficulty_ref);

    total_difficulty = total_difficulty_ref[0];

    blocks[new_block].previous_block = previous_block;
    blocks[new_block].state_hash = computeStateHash(heights, seeds, total_difficulty);
    blocks[new_block].cumulative_difficulty = blocks[previous_block].cumulative_difficulty + total_difficulty;
    if (blocks[new_block].cumulative_difficulty > blocks[best_block].cumulative_difficulty) {
      best_block = new_block;
    }
  }
}
