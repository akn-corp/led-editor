/**
 * Logique d'encodage LEDS (miroir de StateProtocol.cs) — module partagé pour les tests Node.
 */

const path = require("path");
const { LED_HEADER_SIZE, MAX_LED_ENTRIES_PER_CHUNK } = require(
  path.resolve(__dirname, "../../../led-routing-hub/src/core/protocol.js"),
);

const LED_MAGIC = Buffer.from("LEDS", "ascii");

function writeUInt16LE(buf, offset, value) {
  buf.writeUInt16LE(value & 0xffff, offset);
}

function getChunksForEntities(entityIds) {
  const sorted = [...entityIds].sort((a, b) => a - b);
  if (sorted.length === 0) return [];

  const chunks = [];
  let rangeStart = sorted[0];
  let rangeEnd = sorted[0];

  function appendRange(start, end) {
    let cursor = start;
    while (cursor <= end) {
      const count = Math.min(MAX_LED_ENTRIES_PER_CHUNK, end - cursor + 1);
      chunks.push({ startEntityId: cursor, entryCount: count });
      cursor += count;
    }
  }

  for (let i = 1; i < sorted.length; i += 1) {
    if (sorted[i] === rangeEnd + 1) {
      rangeEnd = sorted[i];
    } else {
      appendRange(rangeStart, rangeEnd);
      rangeStart = rangeEnd = sorted[i];
    }
  }
  appendRange(rangeStart, rangeEnd);
  return chunks;
}

function encodeLedsChunk({ frameId, chunkIndex, chunkCount, startEntityId, colors }) {
  const entryCount = colors.length;
  const buf = Buffer.alloc(LED_HEADER_SIZE + entryCount * 3);
  LED_MAGIC.copy(buf, 0);
  buf.writeUInt8(1, 4);
  writeUInt16LE(buf, 5, frameId);
  buf.writeUInt8(chunkIndex & 0xff, 7);
  buf.writeUInt8(chunkCount & 0xff, 8);
  writeUInt16LE(buf, 9, startEntityId);
  writeUInt16LE(buf, 11, entryCount);

  let offset = LED_HEADER_SIZE;
  for (const color of colors) {
    buf.writeUInt8(color.r & 0xff, offset);
    buf.writeUInt8(color.g & 0xff, offset + 1);
    buf.writeUInt8(color.b & 0xff, offset + 2);
    offset += 3;
  }
  return buf;
}

function encodeLedFrame(frameId, entityColors) {
  const entityIds = Object.keys(entityColors).map(Number);
  const chunkDefs = getChunksForEntities(entityIds);
  const chunkCount = chunkDefs.length;

  return chunkDefs.map((chunk, chunkIndex) => {
    const colors = [];
    for (let i = 0; i < chunk.entryCount; i += 1) {
      const entityId = chunk.startEntityId + i;
      colors.push(entityColors[entityId] ?? { r: 0, g: 0, b: 0 });
    }
    return encodeLedsChunk({
      frameId,
      chunkIndex,
      chunkCount,
      startEntityId: chunk.startEntityId,
      colors,
    });
  });
}

function encodeWallLedFrame(frameId, entityColors) {
  const { getAllWallLedChunks } = require("./wall-mapping-lib");
  const chunkDefs = getAllWallLedChunks();
  const chunkCount = chunkDefs.length;

  return chunkDefs.map((chunk, chunkIndex) => {
    const colors = [];
    for (let i = 0; i < chunk.entryCount; i += 1) {
      const entityId = chunk.startEntityId + i;
      colors.push(entityColors[entityId] ?? { r: 0, g: 0, b: 0 });
    }
    return encodeLedsChunk({
      frameId,
      chunkIndex,
      chunkCount,
      startEntityId: chunk.startEntityId,
      colors,
    });
  });
}

function totalEntityCount() {
  const { totalEntityCount: count } = require("./wall-mapping-lib");
  return count();
}

module.exports = {
  getChunksForEntities,
  encodeLedsChunk,
  encodeLedFrame,
  encodeWallLedFrame,
  totalEntityCount,
};
