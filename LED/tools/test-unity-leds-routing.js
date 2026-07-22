#!/usr/bin/env node
/**
 * Test d'intégration LEDS (format Unity mur Glassworks) ↔ led-routing-hub.
 *
 * Modes :
 *   (défaut)  receiver local sur 6456 — ne conflict pas avec `npm run router`
 *   --live    envoie seulement vers :6455 (router déjà lancé, pas de bind)
 *
 * Usage :
 *   node tools/test-unity-leds-routing.js
 *   node tools/test-unity-leds-routing.js --live
 */

const dgram = require("dgram");
const path = require("path");

const hubRoot = path.resolve(__dirname, "../../../led-routing-hub");
const { STATE_PORT } = require(path.join(hubRoot, "src/core/protocol"));
const { loadConfig } = require(path.join(hubRoot, "src/core/config"));
const { createBufferManager } = require(path.join(hubRoot, "src/core/dmxBuffers"));
const { startStateReceiver } = require(path.join(hubRoot, "src/core/stateReceiver"));
const { encodeWallLedFrame, totalEntityCount } = require("./validate-state-protocol-lib");
const { getAllWallLedChunks } = require("./wall-mapping-lib");

const STANDALONE_PORT = 6456;
const TEST_ENTITY_ID = 228;
const args = process.argv.slice(2);
const liveMode = args.includes("--live");

function buildTestFrame() {
  const entityColors = {};
  const chunks = getAllWallLedChunks();
  const minId = chunks[0].startEntityId;
  const maxId = chunks[chunks.length - 1].startEntityId + chunks[chunks.length - 1].entryCount - 1;

  for (let id = minId; id <= maxId; id += 1) {
    entityColors[id] = { r: 0, g: 0, b: 0 };
  }
  entityColors[TEST_ENTITY_ID] = { r: 255, g: 0, b: 0 };

  return encodeWallLedFrame(99, entityColors);
}

function sendPackets(client, packets, targetPort) {
  return new Promise((resolve, reject) => {
    let sent = 0;
    for (const packet of packets) {
      client.send(packet, targetPort, "127.0.0.1", (err) => {
        if (err) reject(err);
        sent += 1;
        if (sent === packets.length) resolve();
      });
    }
  });
}

async function runLive() {
  const packets = buildTestFrame();
  const client = dgram.createSocket("udp4");

  await sendPackets(client, packets, STATE_PORT);
  client.close();

  console.log("OK: paquets LEDS mur Glassworks envoyés vers le router actif");
  console.log(
    `  cible=127.0.0.1:${STATE_PORT}, entités=${totalEntityCount()}, chunks=${packets.length}, frameId=99`,
  );
  console.log(`  test entity ${TEST_ENTITY_ID} = rouge (coin haut-gauche visible)`);
  console.log("  vérifiez les logs du router ([receiver] LED frame=99 …)");
}

async function runStandalone() {
  const config = loadConfig(path.join(hubRoot, "config/mur-led.json"));
  const bufferManager = createBufferManager(config);

  const receiver = startStateReceiver(bufferManager, { port: STANDALONE_PORT });
  await receiver.ready;

  const packets = buildTestFrame();
  const client = dgram.createSocket("udp4");

  await sendPackets(client, packets, STANDALONE_PORT);
  await new Promise((r) => setTimeout(r, 300));

  const stats = receiver.getStats();
  receiver.stop();
  client.close();

  if (stats.ledFrameId !== 99) {
    console.error(`FAIL: frameId LEDS non reçu (stats=${JSON.stringify(stats)})`);
    process.exit(1);
  }

  console.log("OK: stateReceiver local a accepté les paquets LEDS mur Glassworks");
  console.log(
    `  port=${STANDALONE_PORT}, entités=${totalEntityCount()}, chunks=${packets.length}, ledFrameId=${stats.ledFrameId}`,
  );
  console.log(`  test entity ${TEST_ENTITY_ID} = rouge`);
}

async function main() {
  if (liveMode) {
    await runLive();
    return;
  }
  await runStandalone();
}

main().catch((err) => {
  if (err.message && err.message.includes("EADDRINUSE")) {
    console.error("FAIL: port déjà utilisé — arrêtez le router ou utilisez --live");
    console.error("  standalone: npm run test:routing");
    console.error("  router actif: npm run test:routing:live");
  } else {
    console.error("FAIL:", err.message);
  }
  process.exit(1);
});
