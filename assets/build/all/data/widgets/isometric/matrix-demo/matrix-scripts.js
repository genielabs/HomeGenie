/* Smart Light matrix player and scripts from homegenie.it.

MIT License

Copyright (c) 2018-2025 Generoso Martello - G-Labs https://github.com/genielabs

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

class ScriptInterpreter {
  constructor(scriptText, cols, rows) {
    this.cols = cols;
    this.rows = rows;
    this.ledsCount = cols * rows;

    this.pixels = [];
    this.displayPixels = []; // NUOVO: Buffer di output già normalizzato

    // Allocate pixel states
    for (let i = 0; i < this.ledsCount; i++) {
      this.pixels.push({
        h: 0, s: 0, v: 0,
        targetH: 0, targetS: 0, targetV: 0,
        startH: 0, startS: 0, startV: 0,
        transDuration: 0,
        transElapsed: 0,
        isDynamic: false
      });
      // Inizializza il buffer di rendering
      this.displayPixels.push({ h: 0, s: 0, v: 0 });
    }

    this.currentTime = 0;
    this.maxTime = 0;
    this.commands = [];
    this.isFirstFrame = true;
    this.loopStartTime = 0;
    this.parseScript(scriptText);
  }

  parseScript(scriptText) {
    const lines = scriptText.split('\n');
    let lineIndex = 0;
    let captureLoopStart = false;

    lines.forEach(line => {
      const cleanLine = line.replace(/\/\/.*$/, '').trim();
      if (!cleanLine) return;

      if (cleanLine.startsWith('#')) {
        if (cleanLine.startsWith('#LOOP:')) {
        } else if (cleanLine.startsWith('#REPEAT')) {
        } else {
          captureLoopStart = true;
        }
        return;
      }

      const parts = cleanLine.split(/\s+/);
      if (parts.length < 3) return;

      const timeSec = parseFloat(parts[0]);
      const timeMs = timeSec * 1000;
      if (timeSec > this.maxTime) this.maxTime = timeSec;

      if (captureLoopStart) {
        this.loopStartTime = timeMs;
        captureLoopStart = false;
      }

      const selection = parts[1];
      const command = parts.slice(2).join(' ');

      this.commands.push({
        time: timeMs,
        selection,
        command,
        index: lineIndex++
      });
    });

    if (this.maxTime === 0) this.maxTime = 1.0;
    this.maxTimeMs = this.maxTime * 1000;

    this.commands.sort((a, b) => {
      if (a.time !== b.time) return a.time - b.time;
      return a.index - b.index;
    });
  }

  update(deltaTime, masterH, masterS, masterV) {
    // 1. Process active transitions (fades) for each pixel
    for (let i = 0; i < this.ledsCount; i++) {
      const p = this.pixels[i];
      if (p.transDuration > 0 && p.transElapsed < p.transDuration) {
        p.transElapsed += deltaTime;
        if (p.transElapsed >= p.transDuration) {
          p.h = p.targetH;
          p.s = p.targetS;
          p.v = p.targetV;
          p.transDuration = 0;
        } else {
          const t = p.transElapsed / p.transDuration;
          p.h = p.startH + (p.targetH - p.startH) * t;
          p.s = p.startS + (p.targetS - p.startS) * t;
          p.v = p.startV + (p.targetV - p.startV) * t;
        }
      }
    }

    // 2. Advance the timeline clock and check for looping
    let nextTime = this.currentTime + deltaTime;
    let reset = false;

    if (nextTime >= this.maxTimeMs) {
      nextTime = this.loopStartTime + (nextTime - this.maxTimeMs);
      reset = true;
    }

    const oldTime = this.currentTime;
    this.currentTime = nextTime;

    if (reset) {
      this.commands.forEach(cmd => {
        if (cmd.time > oldTime && cmd.time <= this.maxTimeMs) {
          this.executeCommand(cmd.selection, cmd.command, masterH, masterS, masterV);
        }
      });
      this.commands.forEach(cmd => {
        if (cmd.time >= this.loopStartTime && cmd.time <= this.currentTime) {
          this.executeCommand(cmd.selection, cmd.command, masterH, masterS, masterV);
        }
      });
      this.isFirstFrame = false;
    } else {
      this.commands.forEach(cmd => {
        const condition = this.isFirstFrame
          ? (cmd.time >= 0 && cmd.time <= this.currentTime)
          : (cmd.time > oldTime && cmd.time <= this.currentTime);

        if (condition) {
          this.executeCommand(cmd.selection, cmd.command, masterH, masterS, masterV);
        }
      });
      this.isFirstFrame = false;
    }

    // 3. MASTER NORMALIZATION & REAL-TIME OVERRIDE
    for (let i = 0; i < this.ledsCount; i++) {
      const p = this.pixels[i];
      const dp = this.displayPixels[i];

      if (p.isDynamic) {
        // Se è DEFAULT, segue integralmente il colore master in tempo reale
        dp.h = masterH;
        dp.s = masterS;
        dp.v = masterV;
      } else {
        // Se è un colore scriptato, mantiene la sua tinta ma scala la luminosità col master!
        dp.h = p.h;
        dp.s = p.s;
        dp.v = p.v * masterV; // <--- ECCO IL MOLTIPLICATORE (VCA)
      }
    }
  }

  executeCommand(selection, command, masterH, masterS, masterV) {
    let color = { h: 0, s: 0, v: 0, transDuration: 0, isLevelOnly: false, isDynamic: false };

    if (command.toUpperCase() === 'OFF') {
      color = { h: 0, s: 0, v: 0, transDuration: 0, isLevelOnly: false, isDynamic: false };
    } else if (command.toUpperCase() === 'ON' || command.toUpperCase() === 'DEFAULT') {
      // ON e DEFAULT ora attivano entrambi la modalità Dinamica agganciata al Master Color
      color = { h: masterH, s: masterS, v: masterV, transDuration: 0, isLevelOnly: false, isDynamic: true };
    } else if (command.toUpperCase().startsWith('HSB')) {
      const match = command.match(/HSB\(([^)]+)\)/i);
      if (match) {
        const parts = match[1].split(',');
        color = {
          h: parseFloat(parts[0]),
          s: parseFloat(parts[1]),
          v: parseFloat(parts[2]),
          transDuration: parts.length >= 4 ? parseFloat(parts[3]) : 0,
          isLevelOnly: false,
          isDynamic: false
        };
      }
    } else if (command.toUpperCase().startsWith('LEVEL')) {
      const match = command.match(/LEVEL\(([^)]+)\)/i);
      if (match) {
        const parts = match[1].split(',');
        color = {
          h: 0,
          s: 0,
          v: parseFloat(parts[0]) / 100.0,
          transDuration: parts.length >= 2 ? parseFloat(parts[1]) : 0,
          isLevelOnly: true,
          isDynamic: false
        };
      }
    }

    this.applyToPixels(selection, color);
  }

  applyToPixels(selection, color) {
    if (selection.toUpperCase() === 'ALL') {
      for (let i = 0; i < this.ledsCount; i++) {
        this.setPixelColor(i, color);
      }
      return;
    }

    if (selection.includes(':')) {
      const colonIndex = selection.indexOf(':');
      const rowPart = parseInt(selection.substring(0, colonIndex));
      const colSelection = selection.substring(colonIndex + 1);

      let colPart = 0, width = 1, height = 1;

      if (colSelection.includes('+')) {
        const colSubParts = colSelection.split('+');
        colPart = parseInt(colSubParts[0]);
        if (colSubParts[1].includes(':')) {
          const sizeSubParts = colSubParts[1].split(':');
          width = parseInt(sizeSubParts[0]);
          height = parseInt(sizeSubParts[1]);
        } else {
          width = parseInt(colSubParts[1]);
        }
      } else {
        colPart = parseInt(colSelection);
      }

      for (let y = rowPart; y < rowPart + height; y++) {
        for (let x = colPart; x < colPart + width; x++) {
          if (x >= 0 && x < this.cols && y >= 0 && y < this.rows) {
            const idx = (y % 2 === 0) ? (y * this.cols + x) : (y * this.cols + (this.cols - 1 - x));
            this.setPixelColor(idx, color);
          }
        }
      }
    } else if (selection.includes('-')) {
      const parts = selection.split('-');
      const start = parseInt(parts[0]);
      const end = parseInt(parts[1]);
      for (let i = start; i <= end; i++) {
        if (i < this.ledsCount) this.setPixelColor(i, color);
      }
    } else {
      const idx = parseInt(selection);
      if (idx < this.ledsCount) this.setPixelColor(idx, color);
    }
  }

  setPixelColor(idx, color) {
    const p = this.pixels[idx];
    if (!p) return;

    p.isDynamic = color.isDynamic;

    let targetH = color.h;
    let targetS = color.s;
    let targetV = color.v;

    if (color.isLevelOnly) {
      targetH = p.h;
      targetS = p.s;
    }

    if (color.transDuration > 0) {
      p.startH = p.h;
      p.startS = p.s;
      p.startV = p.v;
      p.targetH = targetH;
      p.targetS = targetS;
      p.targetV = targetV;
      p.transDuration = color.transDuration;
      p.transElapsed = 0;
    } else {
      p.h = targetH;
      p.s = targetS;
      p.v = targetV;
      p.targetH = targetH;
      p.targetS = targetS;
      p.targetV = targetV;
      p.transDuration = 0;
    }
  }

  getPixels() {
    return this.displayPixels;
  }
}

const scripts = {
  "ghost": "00.000 ALL Off\n#1\n00.000 1:4+8 DEFAULT\n00.000 2:2+12 DEFAULT\n00.000 3:1+14:10 DEFAULT\n00.000 13:1+3 DEFAULT\n00.000 13:5+6 DEFAULT\n00.000 13:12+3 DEFAULT\n00.000 14:1 DEFAULT\n00.000 14:5+2 DEFAULT\n00.000 14:9+2 DEFAULT\n00.000 14:14 DEFAULT\n00.000 4:4+4:4 HSB(0,0,1,0)\n00.000 4:10+4:4 HSB(0,0,1,0)\n00.000 6:5+2:2 Off\n00.000 6:11+2:2 Off\n#LOOP:1",
  "heart": "00.000 ALL Off\n#1\n00.010 2:3+3 HSB(0,1,1,100)\n00.010 2:10+3 HSB(0,1,1,100)\n00.010 3:9+5 HSB(0,1,1,100)\n00.010 3:2+5 HSB(0,1,1,100)\n00.010 4:1+14 HSB(0,1,1,100)\n00.010 5:1+14 HSB(0,1,1,100)\n00.010 6:1+14 HSB(0,1,1,100)\n00.010 7:2+12 HSB(0,1,1,100)\n00.010 8:3+10 HSB(0,1,1,100)\n00.010 9:4+8 HSB(0,1,1,100)\n00.010 10:5+6 HSB(0,1,1,100)\n00.010 11:6+4 HSB(0,1,1,100)\n00.010 12:7+2 HSB(0,1,1,100)\n00.150 2:3+3 HSB(0,1,0.4,150)\n00.150 2:10+3 HSB(0,1,0.4,150)\n00.150 3:9+5 HSB(0,1,0.4,150)\n00.150 3:2+5 HSB(0,1,0.4,150)\n00.150 4:1+14 HSB(0,1,0.4,150)\n00.150 5:1+14 HSB(0,1,0.4,150)\n00.150 6:1+14 HSB(0,1,0.4,150)\n00.150 7:2+12 HSB(0,1,0.4,150)\n00.150 8:3+10 HSB(0,1,0.4,150)\n00.150 9:4+8 HSB(0,1,0.4,150)\n00.150 10:5+6 HSB(0,1,0.4,150)\n00.150 11:6+4 HSB(0,1,0.4,150)\n00.150 12:7+2 HSB(0,1,0.4,150)\n00.350 2:3+3 HSB(0,1,1,100)\n00.350 2:10+3 HSB(0,1,1,100)\n00.350 3:9+5 HSB(0,1,1,100)\n00.350 3:2+5 HSB(0,1,1,100)\n00.350 4:1+14 HSB(0,1,1,100)\n00.350 5:1+14 HSB(0,1,1,100)\n00.350 6:1+14 HSB(0,1,1,100)\n00.350 7:2+12 HSB(0,1,1,100)\n00.350 8:3+10 HSB(0,1,1,100)\n00.350 9:4+8 HSB(0,1,1,100)\n00.350 10:5+6 HSB(0,1,1,100)\n00.350 11:6+4 HSB(0,1,1,100)\n00.350 12:7+2 HSB(0,1,1,100)\n00.450 2:3+3 HSB(0,1,0.2,400)\n00.450 2:10+3 HSB(0,1,0.2,400)\n00.450 3:9+5 HSB(0,1,0.2,400)\n00.450 3:2+5 HSB(0,1,0.2,400)\n00.450 4:1+14 HSB(0,1,0.2,400)\n00.450 5:1+14 HSB(0,1,0.2,400)\n00.450 6:1+14 HSB(0,1,0.2,400)\n00.450 7:2+12 HSB(0,1,0.2,400)\n00.450 8:3+10 HSB(0,1,0.2,400)\n00.450 9:4+8 HSB(0,1,0.2,400)\n00.450 10:5+6 HSB(0,1,0.2,400)\n00.450 11:6+4 HSB(0,1,0.2,400)\n00.450 12:7+2 HSB(0,1,0.2,400)\n01.200 ALL Off\n#LOOP:1"
};

export function createMatrixScript(effect) {
  return new ScriptInterpreter(scripts[effect], 16, 16);
}
