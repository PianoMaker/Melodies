
  // Enable WEBMIDI.js and trigger the onEnabled() function when ready
    WebMidi
    .enable()
    .then(onEnabled)
    .catch(err => alert(err));

    // Function triggered when WEBMIDI.js is ready
function onEnabled() {

    const main = document.getElementById("main")

    // Display available MIDI input devices
    if (WebMidi.inputs.length < 1) {
        main.innerHTML += "No device detected.";
    } else {
        WebMidi.inputs.forEach((device, index) => {
            main.innerHTML += `${index}: ${device.name} <br>`;
        });
    }
    const mySynth = WebMidi.inputs[0];
    // const mySynth = WebMidi.getInputByName("TYPE NAME HERE!")

    mySynth.channels[1].addListener("noteon", e => {
        document.body.innerHTML += `${e.note.name} <br>`;
    });
  }

