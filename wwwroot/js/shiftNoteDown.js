function shiftNoteDown(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`);
    // ��������� ���� ��� ��������� ��� �� ������ (� ����������� ����� � ������)
    const noteMap = {
        'cis': 'c', 'd': 'cis', 'dis': 'd', 'e': 'dis', 'f': 'e', 'fis': 'f',
        'g': 'fis', 'gis': 'g', 'a': 'gis', 'ais': 'a', 'b': 'a', 'h': 'b',
        'c': 'h', 'as': 'g', 'ges': 'f', 'es': 'd', 'des': 'c'
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
    if (match) {
        let originalNote = match[1]; // �������� ����
        let octaveModifier = match[2];
        let durationModifier = match[3];
        console.log(`match[0] = ${match}, note = ${match[1]}, octave: ${octaveModifier}, duration: ${durationModifier}`);

        // ���� �� ������
        let modifiedNote = noteMap[originalNote] || originalNote; // ����� �� �������� ����
        parts[index] = notesPart.replace(originalNote, modifiedNote); // �������� ���� � �����


        //������� ����� ������
        if (originalNote === 'c' && octaveModifier === undefined) {
            parts[index] = originalNote + "," + durationModifier;
        }
        else if (originalNote === 'c' && octaveModifier.match(/'+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace("'", "");
        }
        else if (originalNote === 'c' && !octaveModifier.match(/,+/))
            parts[index] = modifiedNote + octaveModifier + "," + durationModifier;
    }
    console.log(`changed to ${parts[index]}`);
    // ������� ����� ������� �����
    return parts.join("_");
}
