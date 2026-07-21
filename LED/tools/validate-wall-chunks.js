#!/usr/bin/env node
/**
 * Valide le chunking mur Glassworks (wall-bands) et l'encodage LEDS.
 *
 * Usage : node tools/validate-wall-chunks.js
 */

const path = require("path");
const { decodeLedsChunk } = require(path.resolve(__dirname, "../../../led-routing-hub/src/core/protocol.js"));
const { encodeLedsChunk } = require("./validate-state-protocol-lib");
const { entityIdForCell, getAllWallLedChunks, totalEntityCount } = require("./wall-mapping-lib");

if (entityIdForCell(0, 0) !== 228) {
  console.error("FAIL: entityIdForCell(0,0) attendu 228");
  process.exit(1);
}

if (entityIdForCell(127, 0) !== 101) {
  console.error("FAIL: entityIdForCell(127,0) attendu 101");
  process.exit(1);
}

const chunks = getAllWallLedChunks();
const entityTotal = totalEntityCount();

if (entityTotal !== 16576) {
  console.error(`FAIL: attendu 16576 entités, reçu ${entityTotal}`);
  process.exit(1);
}

if (chunks.length !== 128) {
  console.error(`FAIL: attendu 128 chunks (1 par bande), reçu ${chunks.length}`);
  process.exit(1);
}

const testEntityId = 228;
const colors = new Array(chunks[0].entryCount).fill({ r: 0, g: 0, b: 0 });
const index = testEntityId - chunks[0].startEntityId;
if (index < 0 || index >= colors.length) {
  console.error("FAIL: entité 228 hors du premier chunk");
  process.exit(1);
}
colors[index] = { r: 255, g: 0, b: 0 };

const packet = encodeLedsChunk({
  frameId: 7,
  chunkIndex: 0,
  chunkCount: chunks.length,
  startEntityId: chunks[0].startEntityId,
  colors,
});

const decoded = decodeLedsChunk(packet);
if (decoded.colors[index].r !== 255) {
  console.error("FAIL: entité 228 non rouge dans chunk 0");
  process.exit(1);
}

console.log("OK: mur Glassworks — chunking et encodage compatibles routing-hub");
console.log(`  entités=${entityTotal}, chunks=${chunks.length}, 1er chunk=${chunks[0].entryCount} entrées`);
console.log(`  entity(0,0)=${entityIdForCell(0, 0)}, test entity ${testEntityId} encodable`);
