export function notaname(key) {
    switch (key) {
        case "c": return "до";
        case "cis": return "до♯";
        case "d": return "ре";
        case "dis": return "ре♯";
        case "e": return "мі";
        case "f": return "фа";
        case "fis": return "фа♯";
        case "g": return "соль";
        case "gis": return "соль♯";
        case "a": return "ля";
        case "ais": return "ля♯";
        case "h": return "сі";
        case "b": return "сі♭";
        case "as": return "ля♭";
        case "ges": return "фа♯";
        case "es": return "мі♭";
        case "des": return "ре♭";
        default: return "Невідома нота";
    }
}
