#!/usr/bin/env node
/**
 * Valide l'encodage LEDS (logique portée depuis StateProtocol.cs Unity)
 * contre le décodeur de led-routing-hub.
 *
 * Usage : node tools/validate-state-protocol.js
 */

const path = require("path");
const { decodeLedsChunk } = require(path.resolve(__dirname, "../../../led-routing-hub/src/core/protocol.js"));
const { encodeLedFrame } = require("./validate-state-protocol-lib");

const entityColors = {};
for (let id = 0; id < 1200; id += 1) {
  entityColors[id] = id === 7 ? { r: 255, g: 0, b: 0 } : { r: 0, g: 0, b: 0 };
}

const packets = encodeLedFrame(42, entityColors);

if (packets.length !== 3) {
  console.error(`FAIL: attendu 3 chunks pour 1200 entités, reçu ${packets.length}`);
  process.exit(1);
}

for (const packet of packets) {
  const decoded = decodeLedsChunk(packet);
  if (decoded.frameId !== 42) {
    console.error("FAIL: frameId incorrect");
    process.exit(1);
  }
  if (packet.toString("ascii", 0, 4) !== "LEDS") {
    console.error("FAIL: magic incorrecte");
    process.exit(1);
  }
}

const redChunk = decodeLedsChunk(packets[0]);
const redIndex = 7 - redChunk.startEntityId;
if (redChunk.colors[redIndex].r !== 255) {
  console.error("FAIL: entité 7 non rouge dans le chunk 0");
  process.exit(1);
}

console.log("OK: encodage LEDS compatible routing-hub");
console.log(`  chunks=${packets.length}, tailles=[${packets.map((p) => p.length).join(", ")}]`);
console.log(`  entité 7 = rgb(${redChunk.colors[redIndex].r},${redChunk.colors[redIndex].g},${redChunk.colors[redIndex].b})`);
