import { notaname } from "./notaname.js";

export function shiftNote(index, inputString, direction) {
    console.log(`shiftNote function is working, index = ${index}, direction = ${direction}, keys = ${inputString}`);

    const noteUpMap = {
        'c': 'cis', 'cis': 'd', 'd': 'dis', 'dis': 'e', 'e': 'f', 'f': 'fis',
        'fis': 'g', 'g': 'gis', 'gis': 'a', 'a': 'ais', 'ais': 'h', 'h': 'c',
        'b': 'h', 'as': 'a', 'ges': 'g', 'es': 'e', 'des': 'd'
    };

    const noteDownMap = {
        'cis': 'c', 'd': 'cis', 'dis': 'd', 'e': 'dis', 'f': 'e', 'fis': 'f',
        'g': 'fis', 'gis': 'g', 'a': 'gis', 'ais': 'a', 'b': 'a', 'h': 'b',
        'c': 'h', 'as': 'g', 'ges': 'f', 'es': 'd', 'des': 'c'
    };

    const noteMap = direction === "up" ? noteUpMap : noteDownMap;

    let labels = document.querySelectorAll(".notaname");
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length) {
        console.warn("Індекс виходить за межі доступних елементів.");
        return inputString;
    }

    let notesPart = parts[index];
    let match = notesPart.match(regex);

    if (match) {
        let originalNote = match[1];
        let octaveModifier = match[2] || "";
        let durationModifier = match[3] || "";

        let modifiedNote = noteMap[originalNote] || originalNote;
        parts[index] = notesPart.replace(originalNote, modifiedNote);

        // Перехід через октаву
        if ((originalNote === "h" && direction === "up") || (originalNote === "c" && direction === "down")) {
            if (!octaveModifier) {
                parts[index] = modifiedNote + (direction === "up" ? "'" : ",") + durationModifier;
            } else if (octaveModifier.match(/,+/)) {
                parts[index] = parts[index].replace(",", "");
            } else if (octaveModifier.match(/'+/)) {
                parts[index] = parts[index].replace("'", "");
            }
        }

        if (labels[index]) {
            console.log("changing label")
            labels[index].innerHTML = notaname(modifiedNote);
        }
        else console.log("no label found")
    }

    console.log(`Changed to ${parts[index]}`);
    return parts.join("_");
}

// Використання:
export function shiftNoteUp(index, inputString) {
    return shiftNote(index, inputString, "up");
}

export function shiftNoteDown(index, inputString) {
    return shiftNote(index, inputString, "down");
}
