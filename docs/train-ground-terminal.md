# Terminal tren-terra analògic i GSM-R · simulació

El panell train-ground representa una interfície de cabina inspirada en terminals ferroviaris Alcatel/AEG de generació utilitzada en locomotores RENFE sèrie 252. No és una reproducció certificada ni una còpia exacta del maquinari, firmware, teclat o protocol del terminal instal·lat a cada unitat.

## Funcions simulades
- Mode analògic amb canal, identificació de tren, número de terminal, trucada, recepció, PTT, missatges i alliberament.
- Mode GSM-R amb registre de xarxa local simulat, destinatari, funció ferroviària, trucada entrant/sortint, PTT i registre.
- Botons de teclat numèric, esborrar, emergència prioritària i gestió de missatges.
- Registre cronològic d'esdeveniments i missatges.

## Límits
No hi ha transmissió RF, SIM, autenticació GSM-R, ETCS data radio, EuroRadio, enllaç amb infraestructura ferroviària, ni accés a equips Alcatel/AEG. Els botons només modifiquen l'estat local de la simulació. El botó d'emergència només genera un esdeveniment i missatge de prova: no truca a serveis reals ni actua sobre la locomotora. No connecta amb CS2, JMRI, DCC, tracció, frens o sortides físiques. No s'ha d'utilitzar per comunicacions ferroviàries reals.
