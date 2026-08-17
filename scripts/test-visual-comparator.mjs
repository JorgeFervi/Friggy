import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test, { afterEach } from "node:test";
import { PNG } from "pngjs";
import { compareVisualSnapshots } from "./compare-visual.mjs";

const fixtureDirectories = [];

afterEach(() => {
  for (const directory of fixtureDirectories.splice(0)) {
    fs.rmSync(directory, { recursive: true, force: true });
  }
});

function writePng(filePath, width, height, color) {
  const png = new PNG({ width, height });
  for (let index = 0; index < png.data.length; index += 4) {
    png.data[index] = color.red;
    png.data[index + 1] = color.green;
    png.data[index + 2] = color.blue;
    png.data[index + 3] = 255;
  }

  fs.writeFileSync(filePath, PNG.sync.write(png));
}

function createFixture() {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), "friggy-visual-"));
  fixtureDirectories.push(directory);
  return {
    directory,
    baselinePath: path.join(directory, "baseline.png"),
    actualPath: path.join(directory, "actual.png"),
    diffPath: path.join(directory, "diff.png"),
  };
}

test("informa un baseline ausente", () => {
  const fixture = createFixture();
  writePng(fixture.actualPath, 1, 1, { red: 0, green: 0, blue: 0 });

  assert.throws(
    () => compareVisualSnapshots(fixture),
    /No existe el baseline visual/,
  );
});

test("rechaza dimensiones diferentes", () => {
  const fixture = createFixture();
  writePng(fixture.baselinePath, 1, 1, { red: 0, green: 0, blue: 0 });
  writePng(fixture.actualPath, 2, 1, { red: 0, green: 0, blue: 0 });

  assert.throws(
    () => compareVisualSnapshots(fixture),
    /Las dimensiones no coinciden/,
  );
});

test("acepta una diferencia dentro del ratio permitido", () => {
  const fixture = createFixture();
  writePng(fixture.baselinePath, 1, 1, { red: 0, green: 0, blue: 0 });
  writePng(fixture.actualPath, 1, 1, { red: 255, green: 255, blue: 255 });

  const result = compareVisualSnapshots({ ...fixture, maxDiffRatio: 1 });

  assert.equal(result.mismatchedPixels, 1);
  assert.equal(result.mismatchRatio, 1);
  assert.equal(result.passed, true);
});

test("rechaza una diferencia por encima del ratio permitido", () => {
  const fixture = createFixture();
  writePng(fixture.baselinePath, 1, 1, { red: 0, green: 0, blue: 0 });
  writePng(fixture.actualPath, 1, 1, { red: 255, green: 255, blue: 255 });

  const result = compareVisualSnapshots({ ...fixture, maxDiffRatio: 0 });

  assert.equal(result.passed, false);
  assert.equal(fs.existsSync(fixture.diffPath), true);
});
