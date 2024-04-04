### Finanzplaner Backend Docker-Anleitung

#### Starten der Docker-Container

1. Gehe in den Dateipfad des Projekts:
```bash
   cd Projects/finanzplaner/Backend/FinancePlaner/FinancePlaner
```

3. Starte das Docker-Compose File, umd die API und die Datenbank zu Starten:
```bash
    docker-compose -f docker-compose.yml up
```

5. Sobald beide Container laufen ist Zugriff auf Datenbank möglich
```bash
    docker exec -it financeliquiditymanager-database-1 bash
```
    Mit dem User anmelden und Passwort vergeben (docker-compose.yml):
```bash
    mysql -u newuser -p
```
7. Sollte setup.sql File geändert werden, dann:
```bash
    docker-compose down --volumes
```
    oder
```bash
    docker volume rm financeliquiditymanager_datafiles
```

9. API Zugriff 
    Endpunkt: http://localhost:5200/weatherforecast
    Swagger: http://localhost:5200/swagger/index.html

10. Datenbankzugriff
```bash
    docker exec -it financeliquiditymanager-database-1 bash
```
```bash   
    USE finance;
    SHOW TABLES;
```
