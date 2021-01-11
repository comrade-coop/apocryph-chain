// SPDX-License-Identifier: MIT
pragma solidity >=0.7.0 <0.9.0;

contract KoTH_basic {
  bytes32 public last_block;
  bytes32 public state_hash;
  uint immutable slots_count;
  uint immutable changed_slots_count;

  constructor (
    bytes32 _last_block,
    bytes32 _state_hash,
    uint _slots_count,
    uint _changed_slots_count
  ) {
    last_block = _last_block;
    state_hash = _state_hash;
    slots_count = _slots_count;
    changed_slots_count = _changed_slots_count;
  }

  function compareHeights(
    address signer,
    bytes32 seed,
    bytes32 slot
  ) public pure returns (bool) {
    bytes32 height = keccak256(abi.encodePacked(signer, seed));
    return height >= slot;
  }

  function computeStateHash(
    bytes32[] memory heights, // in
    bytes32[] memory seeds // in
  ) public pure returns (bytes32 hash) {
    return keccak256(abi.encodePacked(heights, seeds));
  }

  function validateSignatures(
    bytes32 new_block,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds, // in
    uint8[] calldata signatures_v,
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_s
  ) internal view {
    bytes32 signed_data = keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n64", last_block, new_block));

    uint signature_count = signatures_v.length;

    uint previous_slot = 0;
    for (uint signature = 0; signature < signature_count; signature ++) {
      address signer = ecrecover(signed_data, signatures_v[signature], signatures_r[signature], signatures_s[signature]);

      uint slot = uint(uint160(signer)) % slots_count;

      require(
        signature == 0 || slot > previous_slot, // signature == 0 check is due to previous_slot being uninitialized on the first iteration
        "Signatures must be given in slot order"); // Check is needed so that a slot cannot be used more than once

      previous_slot = slot;

      bytes32 height = keccak256(abi.encodePacked(signer, seeds[slot]));

      require(
        uint256(height) >= uint256(heights[slot]),
        "Insufficient signature height");

      heights[slot] = height;
    }
  }

  function rotateSeeds(
    bytes32 new_block,
    bytes32[] memory heights, // inout
    bytes32[] memory seeds // inout
  ) internal view {
    bytes32 random_walk_value = new_block;
    for (uint i = 0; i < changed_slots_count; i ++) {
      uint slot = uint256(random_walk_value) % slots_count;
      random_walk_value = keccak256(abi.encodePacked(random_walk_value));
      seeds[slot] = random_walk_value;
      heights[slot] = bytes32(0);
    }
  }

  function setBlock(
    bytes32 new_block,
    bytes32[] memory heights,
    bytes32[] memory seeds,
    uint8[] calldata signatures_v,
    bytes32[] calldata signatures_r,
    bytes32[] calldata signatures_s
  ) external {
    require(state_hash == computeStateHash(heights, seeds) && heights.length == slots_count && seeds.length == slots_count,
      "Invalid state");

    require(signatures_v.length == signatures_r.length && signatures_v.length == signatures_s.length,
      "Signatures length mismatch");

    require(signatures_v.length * 3 >= slots_count * 2,
      "Insufficient signature count");

    validateSignatures(new_block, heights, seeds, signatures_v, signatures_r, signatures_s);
    rotateSeeds(new_block, heights, seeds);

    state_hash = computeStateHash(heights, seeds);
    last_block = new_block;
  }
}
