import { readFile, writeFile, copyFile } from "node:fs/promises";
import sharp from "sharp";

// Keep the approved source PNG intact. ICO entries only change raster size/format.
const source = await readFile(new URL("../public/brand/botzone-icon.png", import.meta.url));
const sizes = [16, 24, 32, 48, 64, 128, 256];
const images = await Promise.all(sizes.map(size => sharp(source).resize(size, size).ensureAlpha().png().toBuffer()));
const directory = Buffer.alloc(6 + sizes.length * 16);
directory.writeUInt16LE(1, 2);
directory.writeUInt16LE(sizes.length, 4);
let offset = directory.length;
for (let index = 0; index < sizes.length; index++) {
  const entry = 6 + index * 16;
  directory[entry] = sizes[index] === 256 ? 0 : sizes[index];
  directory[entry + 1] = directory[entry];
  directory.writeUInt16LE(1, entry + 4);
  directory.writeUInt16LE(32, entry + 6);
  directory.writeUInt32LE(images[index].length, entry + 8);
  directory.writeUInt32LE(offset, entry + 12);
  offset += images[index].length;
}
const destination = new URL("../public/brand/botzone.ico", import.meta.url);
await writeFile(destination, Buffer.concat([directory, ...images]));
await copyFile(destination, new URL("../src/app/favicon.ico", import.meta.url));
console.log(`Packaged BotZone ICO: ${sizes.join(", ")}px (${offset} bytes).`);
