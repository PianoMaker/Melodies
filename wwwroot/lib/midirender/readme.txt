Midirender library for JavaScript

=================================

Copyright (c) 2025 pianomaker (https://github.com/PianoMaker)


Included libraries:
- vexflow (https://github.com/0xfe/vexflow)
- midi-parser-js (https://github.com/colxi/midi-parser-js/)


How to use:	

1) Include the library in your HTML file:

<script src="~/lib/midirender/midifile.bundle.js"></script>
<script src="~/lib/midirender/vexflow.js"></script>
<script src="~/lib/midirender/midiparser.js"></script>
<script src="~/lib/midirender/midiparser_ext.js"></script>
<script src="~/lib/midirender/mr-calculate-rows-helper.js"></script>
<script src="~/lib/midirender/midiRenderer.js"></script>

2) Create a div element in your HTML file for rendering MIDI notation and comments:

<div>    
    <div id="notation"></div>
    <div id="comments"></div>
</div>

3) Use the following JavaScript code to render MIDI notation and comments:
<script> 
    renderMidiFromUrl('/Uploads/example.mid', 'notation', 'comments');
</script>

Full signature with all layout parameters (all have defaults):
<script>
// renderMidiFromUrl(
//   midiUrl,
//   ELEMENT_FOR_RENDERING = 'notation',
//   ELEMENT_FOR_COMMENTS  = 'comments',
//   GENERALWIDTH = 1200,
//   HEIGHT       = 200,
//   TOPPADDING   = 20,
//   BARWIDTH     = 250,
//   CLEFZONE     = 60,
//   Xmargin      = 10
// );

// Example overriding some values:
renderMidiFromUrl('/Uploads/example.mid', 'notation', 'comments', 1000, 180, 15, 220, 60, 12);
</script>

=================================

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.