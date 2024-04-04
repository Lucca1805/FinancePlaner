### Finanzplaner Backend Docker-Anleitung

#### Starten der Docker-Container

1. Gehe in den Dateipfad des Projekts:
   cd Projects/finanzplaner/Backend/FinancePlaner/FinancePlaner

2. Starte das Docker-Compose File, umd die API und die Datenbank zu Starten
    docker-compose -f docker-compose.yml up

3. Sobald beide Container laufen ist Zugriff auf Datenbank möglich
    docker exec -it financeliquiditymanager-database-1 bash
    Mit dem User anmelden und Passwort vergeben (docker-compose.yml)
    mysql -u newuser -p
4. Sollte setup.sql File geändert werden, dann:
    docker-compose down --volumes 
    oder
    docker volume rm financeliquiditymanager_datafiles

5. API Zugriff 
    Endpunkt: http://localhost:5200/weatherforecast
    Swagger: http://localhost:5200/swagger/index.html

6. Datenbankzugriff
    docker exec -it financeliquiditymanager-database-1 bash
    
    USE finance;
    SHOW TABLES;
