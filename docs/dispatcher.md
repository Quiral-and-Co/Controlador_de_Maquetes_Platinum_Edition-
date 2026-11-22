# Dispatcher — JMRI TC Gold Custom 1.1

## Ocupació automàtica amb sensors JMRI

El Dispatcher calcula l'ocupació a partir dels sensors rebuts pel WebSocket JSON de JMRI. Per declarar un bloc Lliure o Ocupat cal registrar el system name exacte, assignar el sensor a una peça del bloc, verificar físicament el sensor i verificar el bloc. Els blocs amb sensors assignats no es poden canviar manualment. Si falta una lectura, es perd la connexió, un sensor no està verificat o l'estat no es reconeix, el bloc passa a Desconegut i no es pot reservar.

## Interpretació

- JMRI ACTIVE (2) → Ocupat.
- JMRI INACTIVE (4) → Lliure.
- Valors nuls, desconeguts o no reconeguts → Desconegut.
- Amb diversos sensors: qualsevol sensor actiu fa el bloc Ocupat; només quan tots són inactius és Lliure.
- L'opció invert d'un sensor verificat inverteix ACTIVE/INACTIVE.
- La pèrdua de connexió buida les lectures i posa a Desconegut els blocs dependents de sensors.

## Proteccions

- No es reserva cap bloc UNKNOWN o OCCUPIED.
- Quan un bloc reservat deixa d'estar FREE, s'allibera tota la ruta afectada.
- Els blocs amb sensors assignats no accepten estats manuals.
- No s'envien ordres DCC, locomotores, desviaments, senyals ni activació de rutes a JMRI. La reserva és local i no és un interbloqueig ferroviari certificat.
- Aquest mòdul no substitueix una parada d'emergència física independent.

## Prova recomanada

1. Amb la maqueta aturada, registra i verifica un sensor.
2. Assigna'l a una peça i al bloc corresponent; verifica també el bloc.
3. Connecta JMRI i comprova que el sensor inactiu dona Lliure.
4. Activa físicament el detector: el bloc ha de passar a Ocupat.
5. Desconnecta JMRI: el bloc ha de passar a Desconegut.
6. Comprova que cap ruta amb aquest bloc es pot reservar mentre l'estat no sigui Lliure.
7. Valida el comportament de cada detector i cablejat abans de qualsevol ús operatiu.


## Gestió de circulacions

Consulta [traffic-management.md](traffic-management.md) per al registre de locomotores Märklin DCC, mode manual/automàtic, pont WiThrottle i interbloqueigs de circulació. Una reserva de tràfic pot incloure el bloc inicial ocupat pel tren; aquest estat ocupat es conserva mentre la circulació és activa. Si un bloc passa a desconegut, s'allibera la reserva i la circulació entra en estat de fallada.
