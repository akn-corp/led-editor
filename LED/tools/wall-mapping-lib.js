/**
 * Port Node de led-studio-editor/src/engine/wall-mapping.ts
 */

const wallBands = require("../Assets/Resources/Configs/wall-bands.json");

const VISIBLE_ROWS = 128;
const COLUMNS_PER_PHYSICAL_BAND = 2;
const ASCENDING_LAST_VISIBLE_OFFSET = 128;
const DESCENDING_FIRST_VISIBLE_OFFSET = 130;
const MAX_LED_ENTRIES_PER_CHUNK = 400;

function entityIdForCell(row, column) {
  if (row < 0 || row >= VISIBLE_ROWS || column < 0 || column >= wallBands.columns) return null;

  const physicalBandIndex = Math.floor(column / COLUMNS_PER_PHYSICAL_BAND);
  const bandIndex = physicalBandIndex * COLUMNS_PER_PHYSICAL_BAND;
  const firstUniverse = wallBands.bands[bandIndex];
  if (!firstUniverse) return null;

  if (column % COLUMNS_PER_PHYSICAL_BAND === 0) {
    return firstUniverse.entityStart + ASCENDING_LAST_VISIBLE_OFFSET - row;
  }
  return firstUniverse.entityStart + DESCENDING_FIRST_VISIBLE_OFFSET + row;
}

function chunkEntityRange(start, end, maxEntries = MAX_LED_ENTRIES_PER_CHUNK) {
  const chunks = [];
  let cursor = start;
  while (cursor <= end) {
    const count = Math.min(maxEntries, end - cursor + 1);
    chunks.push({ startEntityId: cursor, entryCount: count });
    cursor += count;
  }
  return chunks;
}

function getAllWallLedChunks(maxEntries = MAX_LED_ENTRIES_PER_CHUNK) {
  const chunks = [];
  for (const band of wallBands.bands) {
    const end = band.entityStart + band.entityCount - 1;
    chunks.push(...chunkEntityRange(band.entityStart, end, maxEntries));
  }
  return chunks;
}

function totalEntityCount() {
  return wallBands.bands.reduce((sum, band) => sum + band.entityCount, 0);
}

module.exports = {
  wallBands,
  entityIdForCell,
  getAllWallLedChunks,
  totalEntityCount,
};
