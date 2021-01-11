// SPDX-License-Identifier: MIT
pragma solidity >=0.7.0 <0.9.0;

contract KoTH {
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
    slots_count = _slots_count;
    changed_slots_count = _changed_slots_count;

    BlockState storage root_state = blocks[_root_block];
    root_state.previous_block = bytes32(type(uint).max); // max so that we can use previous_block == 0 to check if a block is new
    root_state.state_hash = _root_hash;
    root_state.cumulative_difficulty = 0;

    best_block = _root_hash;
  }

  function computeStateHash(
    bytes32[] memory heights, // in, checked size
    bytes32[] memory seeds, // in, checked size
    uint total_difficulty
  ) public pure returns (bytes32 hash) {
    return keccak256(abi.encodePacked(heights, seeds, total_difficulty));
  }

  function computeEstimatedDifficulty(
    bytes32 height
  ) public pure returns (
    uint difficulty
  ) {
    return (uint(height) / (type(uint).max - uint(height))) >> 4; // >> 4 so that the difficulty values do not overflow as easily
  }

  function ecrecoverPackedSV(
    bytes32 hash,
    bytes32 r,
    bytes32 sv
  ) public pure returns (
    address recovered
  ) {
    uint8 v = 27;
    if (sv & bytes32(0x8000000000000000000000000000000000000000000000000000000000000000) != bytes32(0)) {
      sv &= ~bytes32(0x8000000000000000000000000000000000000000000000000000000000000000);
      v = 28;
    }
    return ecrecover(hash, v, r, sv);
  }

  function validateSignatures(
    bytes32 signed_data,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds, // in
    uint total_difficulty, // inout
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_sv
  ) internal view returns (
    uint _total_difficulty
  ) {
    uint previous_slot = 0;
    for (uint signature = 0; signature < signatures_sv.length; signature ++) {
      address signer = ecrecoverPackedSV(signed_data, signatures_r[signature], signatures_sv[signature]);

      uint slot = uint(uint160(signer)) % slots_count;

      require(
        signature == 0 || slot > previous_slot, // signature == 0 check is due to previous_slot being uninitialized on the first iteration
        "Signatures must be given in slot order");

      previous_slot = slot;

      bytes32 height = keccak256(abi.encodePacked(signer, seeds[slot]));

      bytes32 old_height = heights[slot];
      if (old_height != height) {
        require(
          uint256(height) >= uint256(old_height),
          "Insufficient signature height");

        total_difficulty = total_difficulty - computeEstimatedDifficulty(old_height) + computeEstimatedDifficulty(height); // Can inline?
        heights[slot] = height;
      }
    }
    return total_difficulty;
  }

  function rotateSeeds(
    bytes32 new_block,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds, // inout
    uint total_difficulty // inout
  ) internal view returns (
    uint _total_difficulty
  ) {
    bytes32 random_walk_value = new_block;
    for (uint i = 0; i < changed_slots_count; i ++) {
      uint slot = uint256(random_walk_value) % slots_count;
      random_walk_value = keccak256(abi.encodePacked(random_walk_value));
      seeds[slot] = random_walk_value;
      total_difficulty -= computeEstimatedDifficulty(heights[slot]);
      heights[slot] = bytes32(0);
    }
    return total_difficulty;
  }

  function addBlock(
    bytes32 previous_block,
    bytes32 new_block,
    bytes32[] memory heights,
    bytes32[] memory seeds,
    uint total_difficulty,
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_sv
  ) external {

    BlockState storage block_state = blocks[new_block];
    BlockState storage previous_block_state = blocks[previous_block];

    require(block_state.previous_block == bytes32(0),
      "Block already stored");

    require(previous_block_state.state_hash == computeStateHash(heights, seeds, total_difficulty) && heights.length == slots_count && seeds.length == slots_count,
      "Invalid state");

    require(signatures_sv.length == signatures_r.length,
      "Signatures length mismatch");
    require(signatures_sv.length * 3 >= slots_count * 2,
      "Insufficient signature count");

    bytes32 signed_data = keccak256(abi.encodePacked(previous_block, new_block));

    total_difficulty = validateSignatures(signed_data, heights, seeds, total_difficulty, signatures_r, signatures_sv);
    total_difficulty = rotateSeeds(new_block, heights, seeds, total_difficulty);

    uint cumulative_difficulty = previous_block_state.cumulative_difficulty + total_difficulty;

    block_state.previous_block = previous_block;
    block_state.state_hash = computeStateHash(heights, seeds, total_difficulty);
    block_state.cumulative_difficulty = cumulative_difficulty;

    if (cumulative_difficulty > blocks[best_block].cumulative_difficulty) {
      best_block = new_block;
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
}
