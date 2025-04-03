export function shiftNoteUp(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`);
    // ��������� ���� ��� ��������� ��� �� ������ (� ����������� ����� � ������)
    const noteMap = {
        'c': 'cis', 'cis': 'd', 'd': 'dis', 'dis': 'e', 'e': 'f', 'f': 'fis',
        'fis': 'g', 'g': 'gis', 'gis': 'a', 'a': 'ais', 'ais': 'h', 'h': 'c',
        'b': 'h', 'as': 'a', 'ges': 'g', 'es': 'e', 'des': 'd'
    };

    // ���������� ����� ��� ������ ��� (��������� ����� � �����)
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;

    // ��������� ����� �� �������
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length - 1) {
        console.warn("������ �������� �� ��� ��������� ��������.");
        return inputString; // ���� index �����������, ��������� ����������� �����
    }

    // �������� ����
    let notesPart = parts[index];
    console.log(`found note: ${notesPart}`);
    // ������ ����� ���� � ������
    let match = notesPart.match(regex);
    console.log(`found match: ${match}`);
    if (match) {
        let originalNote = match[1]; // �������� ����
        let octaveModifier = match[2];
        let durationModifier = match[3];

        //���� �� ������
        let modifiedNote = noteMap[originalNote] || originalNote; // ����� �� �������� ����
        parts[index] = notesPart.replace(originalNote, modifiedNote); // �������� ���� � �����


        //������� ����� ������
        if (originalNote === 'h' && octaveModifier === undefined) {
            parts[index] = originalNote + "'" + durationModifier;
        }
        else if (originalNote === 'h' && octaveModifier.match(/,+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace(",", "");
        }
        else if (originalNote === 'h' && !octaveModifier.match(/'+/))
            parts[index] = modifiedNote + octaveModifier + "'" + durationModifier;

    }
    console.log(`changed to ${parts[index]}`);
    // ������� ����� ������� �����
    return parts.join("_");
}
