function changeNoteDuration(index, inputString, durationChangeFn) {
    console.log(`changeNoteDuration function is working, index = ${index}, keys = ${inputString}`);

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
        const durationModifier = match[3];
        let duration = parseInt(durationModifier, 10);

        // ������� ��������� �� ��������� �������� �������
        duration = durationChangeFn(duration);

        let newDuration = duration.toString();
        parts[index] = notesPart.replace(durationModifier, newDuration); // �������� ���� � �����
    }

    console.log(`changed to ${parts[index]}`);
    // ������� ����� ������� �����
    return parts.join("_");
}

export function doubleNote(index, inputString) {
    return changeNoteDuration(index, inputString, (duration) => {
        if (duration > 1) {
            return duration / 2; // ��������� ���������
        }
        return duration;
    });
}

export function halfNote(index, inputString) {
    return changeNoteDuration(index, inputString, (duration) => {
        if (duration <= 32) {
            return duration * 2; // ���������� ���������
        }
        return duration;
    });
}
