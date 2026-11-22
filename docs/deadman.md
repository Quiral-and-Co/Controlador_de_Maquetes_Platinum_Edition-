# Home mort · seqüència de vigilància

- L'usuari assigna una tecla del teclat a la funció de home mort. Per defecte és Espai; la tecla es pot canviar des del pupitre i la selecció queda desada localment al navegador.
- La tecla s'ha de mantenir premuda durant tota la conducció manual. Si es manté premuda, als 30 s s'encén el testimoni blau fix; als 33 s s'activa el testimoni taronja intermitent i el MP3 seleccionat; als 36 s s'ordena la parada d'emergència i s'encén el testimoni vermell fix.
- **Alliberament prematur:** si l'usuari deixa anar la tecla abans dels 30 s, el sistema considera que s'ha arribat als 30 s en aquell mateix instant. Comença immediatament el testimoni blau fix durant 3 s; després, durant 3 s, el taronja intermitent i el senyal acústic; en acabar els 6 s totals, es dispara la parada d'emergència i el vermell queda fix.
- Alliberar la tecla a partir dels 30 s no reinicia ni retarda la seqüència d'avís.
- **Resposta del maquinista:** tornar a prémer la tecla durant la fase blava o taronja confirma la presència, apaga els avisos i reinicia el comptador des de zero. Si no es prem abans d'acabar els 6 s, es dispara l'emergència.
- Si la finestra del navegador perd el focus o la pestanya queda oculta mentre la tecla està premuda, el sistema simula l'alliberament de la tecla i inicia la seqüència d'avís. La pèrdua de heartbeat del navegador durant 2 s mentre la tecla consta com a premuda també provoca l'emergència.
- Després d'un dispar, només es permet rearmament conscient després d'inspecció. La desactivació del watchdog no anul·la les altres portes de seguretat.

**Límit important:** la tecla és una entrada de programari del teclat de l'ordinador, no un dispositiu de seguretat independent. El MP3 es reprodueix al navegador. L'ordre WiThrottle no certifica una frenada física ni garanteix la parada si falla la comunicació o el descodificador. Cal un circuit d'emergència independent que talli la tracció i validar-lo físicament abans d'habilitar DCC.
