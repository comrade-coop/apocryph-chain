const ethereumUtil = require('ethereumjs-util')
const KoTH = artifacts.require("KoTH")

const nullHash = web3.utils.toTwosComplement(0)
const maxHash = web3.utils.toTwosComplement(-1)
const maxHashNum = web3.utils.toBN(maxHash)
const highBit = '0x8000000000000000000000000000000000000000000000000000000000000000'
const highBitNum = web3.utils.toBN(highBit)

function computeStateHash(contractHeights, contractSeeds, contractDifficulty) {
  return web3.utils.soliditySha3({type: 'bytes32[]', value: contractHeights}, {type: 'bytes32[]', value: contractSeeds}, {type: 'uint', value: web3.utils.toHex(contractDifficulty)})
}

function signHash(account, hash) {
  var signature = ethereumUtil.ecsign(Buffer.from(hash.substring(2), 'hex'), Buffer.from(account.privateKey.substring(2), 'hex'), undefined)
  return {k: account.address, s: '0x' + signature.s.toString('hex'), r: '0x' + signature.r.toString('hex'), v: '0x' + signature.v.toString(16)}
}

/*function packSV(s, v) {
  v = web3.utils.hexToNumber(v)
  s = web3.utils.toBN(s)
  assert(s.and(highBitNum).isZero(), web3.utils.toHex(s) + ' should have high bit of 0')
  if (v == 27) {
    return web3.utils.toHex(s)
  } else if (v == 28) {
    return web3.utils.toHex(s.or(highBitNum))
  }
}*/

function packSV(s, v) { // Doing cryptography with hex strings, as web3.utils.toBN/toHex strips off leading zeroes
  v = web3.utils.hexToNumber(v)
  assert(s.length == 32*2 + 2 && s.substring(0, 2) == '0x')

  var highByte = parseInt(s.substring(2, 4), 16)
  assert((highByte & 0x80) == 0, 's should have a high bit of zero ' + highByte + ' (' + s.substring(2, 4) + ')')

  if (v == 27) {
    return s
  } else if (v == 28) {
    return '0x' + (highByte | 0x80).toString(16) + s.substring(4)
  }
}

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
    return web3.utils.BN.min(heightNum.div(maxHashNum.sub(heightNum)), maxHashNum).shrn(4)
  }

  estimateTotalDifficulty(h) {
    var diffs = (h || this.heights).map(x => this.estimateDifficulty(x))
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
    var signedHash = web3.utils.soliditySha3({type: 'bytes32[]', value: this.lastBlock}, {type: 'bytes32[]', value: newBlock})
    var signatures = this.keys.filter(x => x).map(x => signHash(x, signedHash))
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

contract("KoTH", ([creator, relayer]) => {
  var network = new SimulatedNetwork()
  var koth = new SimulatedKoTH(network.getNextBlock(), 10, 4)
  console.log('Initially mined:', koth.mine(5000), koth.estimateTotalDifficulty().toString(10))

  it("should recover the correct key when using combined S+V", async () => {
    var staticInstance = await KoTH.new(maxHash, maxHash, 1, 1, {from: creator})

    var key = web3.eth.accounts.create()
    for (var i = 0; i < 10; i++) {
      var messageHash = web3.utils.sha3('i: ' + i)
      var {v, r, s} = signHash(key, messageHash)

      var recovered = web3.eth.accounts.recover({messageHash, v, r, s})

      assert.equal(recovered, key.address)

      var sv = packSV(s, v)

      var recoveredContract = await staticInstance.ecrecoverPackedSV.call(messageHash, r, sv)

      assert.equal(recoveredContract, key.address)
    }
  })

  it("should be able to process multiple blocks", async () => {
    var contractHeights = koth.heights.slice()
    var contractSeeds = koth.seeds.slice()
    var contractDifficulty = koth.estimateTotalDifficulty(contractHeights)
    var initialState = computeStateHash(contractHeights, contractSeeds, contractDifficulty)

    var instance = await KoTH.new(koth.lastBlock, initialState, koth.slotCount, koth.changedSlotCount, {from: creator})

    var blocks = []

    for (var i = 0; i < 5; i++) {
      console.log('Mined:', koth.mine(1000), koth.estimateTotalDifficulty().toString(10))

      var nextBlock = network.getNextBlock()

      var signatures = koth.signBlock(nextBlock)
      var neededSignatures = Math.ceil(koth.slotCount * 2 / 3)
      while (signatures.length > neededSignatures) {
        signatures.splice(Math.floor(Math.random() * signatures.length), 1)
      }

      await instance.addBlock(koth.lastBlock, nextBlock, contractHeights, contractSeeds, contractDifficulty, signatures.map(x=>x.r), signatures.map(x=>packSV(x.s, x.v)), {from: relayer})

      for (var signature of signatures) {
        let slot = koth.getKeySlot(signature.k)
        contractHeights[slot] = koth.getKeyHeight(signature.k, slot)
      }

      var rotatedSeeds = koth.rotateSeeds(nextBlock)
      for (var [slot, seed] of rotatedSeeds) {
        contractSeeds[slot] = seed
        contractHeights[slot] = nullHash
      }
      contractDifficulty = koth.estimateTotalDifficulty(contractHeights)

      var storedBlock = await instance.best_block.call()
      assert.equal(storedBlock, nextBlock)

      var storedState = await instance.blocks.call(nextBlock)
      var computedState = computeStateHash(contractHeights, contractSeeds, contractDifficulty)
      assert.equal(storedState.state_hash, computedState)

      blocks.push([nextBlock, contractDifficulty])
    }

    var confirmationDifficulty = blocks.pop()[1];
    blocks.reverse()
    for (var [block, totalDifficulty] of blocks) {
      var confirmedBlock = await instance.getConfirmedBlockDiffBrute.call(confirmationDifficulty)
      assert.equal(confirmedBlock, block)
      confirmationDifficulty = confirmationDifficulty.add(totalDifficulty)
    }
  })
})
