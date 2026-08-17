import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import pixelmatch from "pixelmatch";
import { PNG } from "pngjs";

export function compareVisualSnapshots({
  baselinePath,
  actualPath,
  diffPath,
  maxDiffRatio = 0.002,
  threshold = 0.15,
}) {
  if (!fs.existsSync(baselinePath)) {
    throw new Error(`No existe el baseline visual: ${baselinePath}`);
  }

  if (!fs.existsSync(actualPath)) {
    throw new Error(`No existe la captura actual: ${actualPath}`);
  }

  const baseline = PNG.sync.read(fs.readFileSync(baselinePath));
  const actual = PNG.sync.read(fs.readFileSync(actualPath));

  if (baseline.width !== actual.width || baseline.height !== actual.height) {
    throw new Error(
      `Las dimensiones no coinciden: baseline ${baseline.width}x${baseline.height}, ` +
        `actual ${actual.width}x${actual.height}.`,
    );
  }

  const diff = new PNG({ width: baseline.width, height: baseline.height });
  const mismatchedPixels = pixelmatch(
    baseline.data,
    actual.data,
    diff.data,
    baseline.width,
    baseline.height,
    {
      threshold,
      includeAA: false,
      alpha: 0.8,
    },
  );
  const totalPixels = baseline.width * baseline.height;
  const mismatchRatio = mismatchedPixels / totalPixels;

  fs.mkdirSync(path.dirname(diffPath), { recursive: true });
  fs.writeFileSync(diffPath, PNG.sync.write(diff));

  return {
    mismatchedPixels,
    mismatchRatio,
    maxDiffRatio,
    passed: mismatchRatio <= maxDiffRatio,
  };
}

function readArguments(argumentsList) {
  if (argumentsList.length !== 3) {
    throw new Error(
      "Uso: node scripts/compare-visual.mjs <baseline.png> <actual.png> <diff.png>",
    );
  }

  return {
    baselinePath: path.resolve(argumentsList[0]),
    actualPath: path.resolve(argumentsList[1]),
    diffPath: path.resolve(argumentsList[2]),
  };
}

const currentScript = fileURLToPath(import.meta.url);
const invokedScript = process.argv[1] ? path.resolve(process.argv[1]) : undefined;
if (invokedScript === currentScript) {
  try {
    const result = compareVisualSnapshots(readArguments(process.argv.slice(2)));
    process.stdout.write(`${JSON.stringify(result)}\n`);
    if (!result.passed) {
      process.exitCode = 1;
    }
  } catch (error) {
    process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
    process.exitCode = 2;
  }
}
