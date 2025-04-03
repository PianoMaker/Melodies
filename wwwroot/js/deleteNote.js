export function deleteNote(index, inputString) {

    console.log(`delete function is working, index = ${index}, keys = ${inputString}`);

    // ���������� ����� ��� ������ ��� (��������� ����� � �����)
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;

    // ��������� ����� �� �������
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length - 1) {
        console.warn("������ �������� �� ��� ��������� ��������.");
        return inputString; // ���� index �����������, ��������� ����������� �����
    }

    parts[index] = ""; // �������� ����� ����
    return parts.join("_");
}
