const KoTH = artifacts.require("KoTH_basic")

const nullHash = web3.utils.toTwosComplement(0)
const maxHash = web3.utils.toTwosComplement(-1)
const maxHashNum = web3.utils.toBN(maxHash)

class SimulatedNetwork {
  constructor() {
    this.i = 0
  }

  getNextBlock() {
    this.i ++
    return web3.utils.keccak256(new web3.utils.BN(this.i))
  }
}

class SimulatedKoTH {
  constructor(initialBlock, slotCount, changedSlotCount) {
    this.lastBlock = initialBlock
    this.slotCount = slotCount
    this.changedSlotCount = changedSlotCount
    this.keys = Array(this.slotCount).fill()
    this.seeds = Array(this.slotCount).fill(nullHash)
    this.heights = Array(this.slotCount).fill(nullHash)
  }

  getKeySlot(address) {
    return web3.utils.toBN(address).modn(this.slotCount)
  }

  getKeyHeight(address, slot) {
    return web3.utils.soliditySha3({type: 'address', value: address}, {type: 'bytes32', value: this.seeds[slot]})
  }

  compareHeights(heightA, heightB) { // true if A>B
    return heightA >= heightB // web3.utils.toBN(heightA).gte(web3.utils.toBN(heightB))
  }

  estimateDifficulty(height) {
    var heightNum = web3.utils.toBN(height)
    return web3.utils.BN.min(heightNum.div(maxHashNum.sub(heightNum)), maxHashNum)
  }

  estimateTotalDifficulty() {
    var diffs = this.heights.map(x => this.estimateDifficulty(x))
    return diffs.reduce((a, b) => a.add(b))
  }

  mine(n) {
    let changed = 0
    for(var i = 0; i < n; i++) {
      var key = web3.eth.accounts.create()
      var slot = this.getKeySlot(key.address)
      var height = this.getKeyHeight(key.address, slot)
      if (this.compareHeights(height, this.heights[slot])) {
        changed ++
        this.heights[slot] = height
        this.keys[slot] = key
      }
    }
    return changed
  }

  signBlock(newBlock) {
    var signedData = this.lastBlock + newBlock.substring(2); // Ah, the glories of doing cryptography with hex strings
    var signatures = this.keys.filter(x => x).map(x => {let s = x.sign(signedData); s.k = x.address; return s})
    if (signatures.length * 3 < this.slotCount * 2) return null
    return signatures
  }

  rotateSeeds(newBlock) {
    var result = []
    var randomWalkValue = newBlock
    for (var i = 0; i < this.changedSlotCount; i ++) {
      var slot = web3.utils.toBN(randomWalkValue).modn(this.slotCount)
      randomWalkValue = web3.utils.soliditySha3({type: 'bytes32', value: randomWalkValue})
      this.seeds[slot] = randomWalkValue
      this.heights[slot] = nullHash
      this.keys[slot] = undefined
      result.push([slot, randomWalkValue])
    }
    this.lastBlock = newBlock
    return result
  }
}

contract("KoTH_basic", ([creator, relayer]) => {
  var network = new SimulatedNetwork()
  var koth = new SimulatedKoTH(network.getNextBlock(), 10, 1)
  console.log('Initially mined:', koth.mine(2000), koth.estimateTotalDifficulty().toString(10))

  it("should work...", async () => {
    var contractHeights = koth.heights.slice()
    var contractSeeds = koth.seeds.slice()
    var initialState = web3.utils.soliditySha3({type: 'bytes32[]', value: contractHeights}, {type: 'bytes32[]', value: contractSeeds})

    var instance = await KoTH.new(koth.lastBlock, initialState, koth.slotCount, koth.changedSlotCount, {from: creator})

    for (var i = 0; i < 5; i++) {
      console.log('Mined:', koth.mine(1000), koth.estimateTotalDifficulty().toString(10))

      var nextBlock = network.getNextBlock()
      var signatures = koth.signBlock(nextBlock)
      var neededSignatures = Math.ceil(koth.slotCount * 2 / 3)
      while (signatures.length > neededSignatures) {
        signatures.splice(Math.floor(Math.random() * signatures.length), 1)
      }

      await instance.setBlock(nextBlock, contractHeights, contractSeeds, signatures.map(x=>x.v), signatures.map(x=>x.r), signatures.map(x=>x.s), {from: relayer})

      for (var signature of signatures) {
        let slot = koth.getKeySlot(signature.k)
        contractHeights[slot] = koth.getKeyHeight(signature.k, slot)
      }
      var rotatedSeeds = koth.rotateSeeds(nextBlock)
      for (var [slot, seed] of rotatedSeeds) {
        contractSeeds[slot] = seed
        contractHeights[slot] = nullHash
      }

      var storedBlock = await instance.last_block.call()
      assert.equal(storedBlock, nextBlock)

      var storedState = await instance.state_hash.call()
      var computedState = web3.utils.soliditySha3({type: 'bytes32[]', value: contractHeights}, {type: 'bytes32[]', value: contractSeeds})
      assert.equal(storedState, computedState)
    }
  })
})
