```mermaid
graph TD
    Z[renderMidiFromUrl] -->|"fetch MIDI"| A
    A[DrawScore] --> B[RenderMidiFileToNotation]
    B --> C[RenderMeasures]
    
    subgraph "Цикл по всіх тактах"
        C -->|"Наступний такт"| D[RenderMeasure]
        D --> M[getKeySignatureChanges]
        M -->|"Знаки при ключі"| D
        
        D -->|"Цикл по MIDI подіях"| E["Перетворення в notes[], ties[]"]
        E --> N[midiNoteToVexFlowWithKey]
        N -->|"Висота ноти + альтерація"| O[decideAccidentalForNote]
        O -->|"Остаточний знак"| E
        
        E -->|"Кінець подій"| F{"Є активні ноти?"}
        F -->|"Ні"| G[addMissingRests - паузи]
        F -->|"Так"| H[DrawMeasure]
        G --> H
        
        subgraph "Малювання елементів такту"
            H -->|"1"| I[drawBeams - ребра між нотами]
            I -->|"Намальовано"| H
            H -->|"2"| J[drawTies - ліги між нотами]
            J -->|"Намальовано"| H
            H -->|"3"| K[drawTuplets - тріолі]
            K -->|"Намальовано"| H
        end
        
        H -->|"Такт готовий"| C
    end
    
    C -->|"Всі такти оброблені"| L[Готовий SVG]
    
    subgraph "Пост-обробка SVG"
        L --> P[calculateRequiredHeight]
        P --> Q[adjust SVG розміри]
        Q -->|"1"| R["svg.getBBox()"]
        R -->|"2"| S["встановити viewBox"]
        S -->|"3"| T["оновити height"]
        T -->|"4"| U["оновити стилі контейнера"]
    end
    
    U --> V[Фінальний SVG]