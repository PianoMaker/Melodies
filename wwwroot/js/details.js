    function detailsModule() {
      // Safely run only when DOM loaded and midi parsing libs are present
      function midiPitchToSearchToken(pitch) {
        const sharpNames = ["C","C#","D","D#","E","F","F#","G","G#","A","A#","B"];
        const pc = ((pitch % 12) + 12) % 12;
        const name = sharpNames[pc];
        if (name === "B") return "h";            // Ukrainian 'h' for B
        if (name.includes("#")) return name[0].toLowerCase() + "is"; // C# -> cis
        return name.toLowerCase();               // C -> c, D -> d, etc.
      }

      function appendAntiForgeryToken(form) {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput && tokenInput.value) {
          const cloned = document.createElement("input");
          cloned.type = "hidden";
          cloned.name = "__RequestVerificationToken";
          cloned.value = tokenInput.value;
          form.appendChild(cloned);
          return true;
        }
        return false;
      }

      function postKeysToSearch(actionUrl, keys, numerator, denominator) {
        const form = document.createElement("form");
        form.method = "post";
        form.action = actionUrl;

        const inputKeys = document.createElement("input");
        inputKeys.type = "hidden";
        inputKeys.name = "Keys";
        inputKeys.value = keys;
        form.appendChild(inputKeys);

        if (typeof numerator !== "undefined") {
          const inNum = document.createElement("input");
          inNum.type = "hidden";
          inNum.name = "TimeSignatureNumerator";
          inNum.value = String(numerator);
          form.appendChild(inNum);
        }

        if (typeof denominator !== "undefined") {
          const inDen = document.createElement("input");
          inDen.type = "hidden";
          inDen.name = "TimeSignatureDenominator";
          inDen.value = String(denominator);
          form.appendChild(inDen);
        }

        // include antiforgery token if present
        appendAntiForgeryToken(form);

        document.body.appendChild(form);
        form.submit();
      }

      // convert a duration code like "q", "q.", "8", "16" or "qt" -> numeric string "4", "4.", "8", "16", "4t"
      function durationCodeToNumeric(dc) {
        if (!dc) return "4";
        const dotted = dc.endsWith('.');
        const triplet = dc.endsWith('t');
        const base = (dotted || triplet) ? dc.slice(0, -1) : dc;
        let num = null;
        if (typeof reverseDurationMapping !== 'undefined') {
          num = reverseDurationMapping[base];
        }
        // fallback mapping
        if (!num) {
          const fallback = { w: 1, h: 2, q: 4, "8": 8, "16": 16, "32": 32 };
          num = fallback[base] || 4;
        }
        let res = String(num);
        if (dotted) res += '.';
        if (triplet) res += 't';
        return res;
      }

      async function analyzeMidiAndSearch(midiUrl, actionUrl) {
        try {
          const resp = await fetch(midiUrl);
          if (!resp.ok) throw new Error("Failed to fetch MIDI: " + resp.status);
          const buf = await resp.arrayBuffer();
          const uint8 = new Uint8Array(buf);

          if (typeof MidiParser === "undefined" || typeof SetEventsAbsoluteTime === "undefined") {
            console.warn("Required MIDI parsing helpers not present.");
            window.location.href = actionUrl; // fallback
            return;
          }

          // parse midi and get absolute-time events
          const midiData = MidiParser.Uint8(uint8);
          let allEvents = SetEventsAbsoluteTime(midiData) || [];
          if (typeof ensureEndEvent === "function") ensureEndEvent(allEvents);

          // determine ticks per beat (TPQN)
          let ticksPerBeat = null;
          try {
            if (Array.isArray(midiData.timeDivision)) {
              // frames-per-second mode: second element is ticks per frame; fallback
              ticksPerBeat = midiData.timeDivision[1] || 480;
            } else {
              ticksPerBeat = midiData.timeDivision || 480;
            }
          } catch (e) {
            ticksPerBeat = 480;
          }

          // Extract time signature if present (meta 0x58) - optional
          let numerator, denominator;
          try {
            const tsEvent = allEvents.find(e => e && e.type === 0xFF && e.metaType === 0x58);
            if (tsEvent && Array.isArray(tsEvent.data)) {
              numerator = tsEvent.data[0];
              denominator = Math.pow(2, tsEvent.data[1]);
            }
          } catch (e) { /* ignore */ }

          // sort events to process in temporal order and prefer OFF before ON at same tick
          function rank(ev) {
            if (!ev) return 2;
            if (ev.type === 0x8) return 0;
            if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0) return 0; // NoteOn vel=0 => off
            if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) return 1;
            return 2;
          }
          allEvents.sort((a,b) => (a.absTime - b.absTime) || (rank(a) - rank(b)));

          // Build tokens with durations: map active notes and compute durationTicks at NoteOff
          const tokens = [];
          const active = {}; // pitch -> startAbsTime

          for (const ev of allEvents) {
            if (!ev) continue;
            // NoteOn (type 0x9) with velocity > 0
            if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) {
              const pitch = Number(ev.data[0]);
              // start tracking if not already active (support simple monophonic extraction)
              if (active[pitch] === undefined) active[pitch] = ev.absTime;
            }
            // NoteOff: either explicit 0x8 or NoteOn with vel=0
            else if ((ev.type === 0x8) || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0)) {
              const pitch = Number(ev.data[0]);
              const start = active[pitch];
              if (start !== undefined) {
                const durationTicks = Math.max(0, (ev.absTime || 0) - start);
                // convert ticks -> duration codes (array). prefer the first (largest) piece
                let durCodes = [];
                if (typeof getDurationFromTicks === 'function') {
                  try {
                    durCodes = getDurationFromTicks(durationTicks, ticksPerBeat) || [];
                  } catch (e) {
                    durCodes = [];
                  }
                }
                const primary = durCodes.length > 0 ? durCodes[0] : "q";
                const numeric = durationCodeToNumeric(primary); // e.g. "4" or "4." or "8"
                const noteToken = midiPitchToSearchToken(pitch);
                tokens.push(`${noteToken}${numeric}`);
                // clear active
                delete active[pitch];
              }
            }
          }

          // For any still-active notes (no NoteOff) close them at last absTime
          if (Object.keys(active).length > 0) {
            const lastAbs = allEvents.length ? allEvents[allEvents.length -1].absTime || 0 : 0;
            for (const pitchStr of Object.keys(active)) {
              const start = active[pitchStr];
              const durationTicks = Math.max(0, lastAbs - start);
              let durCodes = [];
              if (typeof getDurationFromTicks === 'function') {
                try { durCodes = getDurationFromTicks(durationTicks, ticksPerBeat) || []; } catch(e){ durCodes = []; }
              }
              const primary = durCodes.length > 0 ? durCodes[0] : "q";
              const numeric = durationCodeToNumeric(primary);
              const noteToken = midiPitchToSearchToken(Number(pitchStr));
              tokens.push(`${noteToken}${numeric}`);
            }
          }

          if (tokens.length === 0) {
            window.location.href = actionUrl;
            return;
          }

          // join with underscore as required by site format
          const keysString = tokens.join("_");
          postKeysToSearch(actionUrl, keysString, numerator, denominator);
        } catch (err) {
          console.error("details.js analyzeMidiAndSearch failed", err);
          try { window.location.href = actionUrl; } catch (e) { /* swallow */ }
        }
      }

      function init() {
        const btn = document.getElementById("findSimilarBtn");
        if (!btn) return;
        btn.addEventListener("click", function (ev) {
          ev.preventDefault();
          const actionUrl = btn.getAttribute("data-search-url");
          const notationEl = document.getElementById("notation");
          const midiUrl = notationEl ? notationEl.dataset.midiUrl : null;
          if (!midiUrl) {
            window.location.href = actionUrl || "/Melodies/Search";
            return;
          }
          analyzeMidiAndSearch(midiUrl, actionUrl || "/Melodies/Search");
        });
      }

      if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
      } else {
        init();
      }
    }

    detailsModule();