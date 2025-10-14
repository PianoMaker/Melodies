```mermaid
graph TD
    subgraph "Завантаження сторінки"
        A[DOMContentLoaded] --> B["Ініціалізація MIDIPlayer"]
        A --> C["Прикріплення обробників подій"]
    end

    subgraph "Відображення нот"
        D[MelodyList.loadMelody] -->|"AJAX запит"| E["Отримання MIDI файлу"]
        E --> F[renderMidiFromUrl]
        
        F --> G["Рендеринг нот"]
        G -->|"1"| H["Відображення у #notation"]
        G -->|"2"| I["Повідомлення у #comments"]
        
        J["Прокрутка до початку такту"] --> K["scrollToBar()"]
    end
    
    subgraph "Управління програванням"
        L[Play] -->|"Клік"| M["startPlayback()"]
        N[Stop] -->|"Клік"| O["stopPlayback()"]
        P[Pause] -->|"Клік"| Q["pausePlayback()"]
        
        M --> R["MIDIPlayer.play()"]
        R --> S["Підсвічування нот"]
        R --> T["Оновлення прогресбару"]
        
        U["Прогресбар"] -->|"Зміна"| V["seekToPosition()"]
    end
    
    subgraph "Інтерактивні елементи"
        W["Вибір темпу"] -->|"Зміна"| X["setTempo()"]
        AA["Кнопки навігації"] -->|"Клік"| BB["prevBar()/nextBar()"]
        BB --> K
    end

    subgraph "Події програвання"
        CC["MIDIPlayer.onNote"] --> DD["Підсвітити ноту"]
        EE["MIDIPlayer.onFinished"] --> FF["Скинути стан"]
    end

    A["Melodies/Details DOMContentLoaded"] --> B["Resolve midiUrl (from model or data-attributes)"]
    B --> C["renderMidiFromUrl(midiUrl, ..., notationId, commentsId)"]
    C --> D["renderMidiFileToNotation(uint8, targets, layout)"]
    D --> E["createMeasureMap()"]
    D --> F["groupEventsByMeasure()"]
    D --> G["calculateRequiredHeight()"]
    D --> H["renderMeasures()"]

    subgraph "Per measure in renderMeasures"
        H --> H1["getKeySignatureChanges()"]
        H --> H2["processActiveNotesFromPreviousBar()"]
        H --> H3["renderMeasure()  (inner function)"]
        H3 --> H3a["midiNoteToVexFlowWithKey()"]
        H3 --> H3b["decideAccidentalForNote()"]
        H3 --> H3c["addRestsBetween() / AddStartRest()"]
        H3 --> H3d["update activeNotes / build notes[], ties[]"]
        H --> H4{"Active notes at bar end?"}
        H4 -->|Так| H5["drawActiveNotes()"]
        H4 -->|Ні| H6["addMissingRests()"]
        H5 --> H7["correctExtraNotes()"]
        H6 --> H7["correctExtraNotes()"]
        H7 --> H8["drawMeasure()"]
        subgraph "drawMeasure()"
            H8 --> H8a["calculateBeams() / drawBeams()"]
            H8 --> H8b["drawTies()"]
            H8 --> H8c["drawTuplets()"]
        end
    end

    D --> I["Post-processing SVG"]
    I --> I1["adjust via svg.getBBox()"]
    I1 --> I2["set viewBox / update height / container styles"]
    I2 --> J["Final SVG in notation container"]