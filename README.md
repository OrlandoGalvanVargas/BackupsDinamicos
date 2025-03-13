### Documentación del Proyecto: Implementación de Backups en SQL Server

---

#### **Descripción General**
Este proyecto tiene como objetivo diseñar e implementar un procedimiento almacenado en SQL Server que permita la ejecución de backups completos, diferenciales y de logs de transacciones. Los backups se almacenarán en ubicaciones organizadas tanto en una infraestructura **Windows Server** como en un entorno **Docker**. Además, se implementará una interfaz web para facilitar la gestión y ejecución de los backups.

---

### **Requisitos del Proyecto**

#### **Parte 1: Implementación del Stored Procedure para Backups**
1. **Parámetros del Procedimiento Almacenado**:
    - `@DatabaseName`: Nombre de la base de datos a respaldar.
    - `@BackupType`: Tipo de backup a realizar (`FULL`, `DIFFERENTIAL`, `LOG`).
    - `@CustomPath`: Ruta personalizada para almacenar el backup (opcional).
    - `@BackupName`: Nombre personalizado para el archivo de backup (opcional).

2. **Funcionalidades del Procedimiento**:
    - Validar que la base de datos exista antes de ejecutar el backup.
    - Crear una estructura de carpetas organizada:
    ```
    \Backups\<DatabaseName>\Full\
    \Backups\<DatabaseName>\Differential\
    \Backups\<DatabaseName>\Log\
    ```
    - Generar nombres de archivos con timestamp para evitar sobrescritura.
    - Permitir la ejecución de backups en dos entornos:
        - **Windows Server**: Almacenar en un directorio local.
        - **Docker**: Montar un volumen en el contenedor para almacenar backups.

---

#### **Parte 2: Implementación en Infraestructura**
1. **Windows Server**:
    - Los backups se almacenan en un directorio local, por ejemplo: `C:\Backups\`.
    - Se crean subcarpetas por base de datos y tipo de backup.

2. **Docker**:
    - Se utiliza un contenedor especializado para almacenamiento de backups.
    - Se monta un volumen en el contenedor para almacenar los backups, por ejemplo: `/var/opt/mssql/backups/`.
    - La estructura de carpetas se mantiene igual que en Windows Server.

---

#### **Parte 3: Automatización y Recuperación**
1. **Automatización**:
    - Programar la ejecución del procedimiento almacenado en **SQL Server Agent** con la siguiente frecuencia:
        - **Backup completo**: Cada 24 horas.
        - **Backup diferencial**: Cada 8 horas.
        - **Backup de logs**: Cada 15 minutos.

2. **Pruebas de Recuperación**:
    - Implementar pruebas periódicas para validar que los backups generados son utilizables en caso de desastre.
    - Verificar la integridad de los archivos de backup y su capacidad de restauración.

---

### **Procedimiento Almacenado**

#### **Código del Stored Procedure**
```sql
USE DBAdmin;
GO

CREATE PROCEDURE sp_BackupDatabase
    @DatabaseName NVARCHAR(128),
    @BackupType NVARCHAR(20),
    @CustomPath NVARCHAR(512) = NULL,
    @BackupName NVARCHAR(128) = NULL
AS
BEGIN
    -- Validar que la base de datos exista
    IF DB_ID(@DatabaseName) IS NULL
    BEGIN
        RAISERROR('La base de datos especificada no existe.', 16, 1);
        RETURN;
    END

    -- Determinar la ruta de backup
    DECLARE @BackupPath NVARCHAR(512);

    IF @CustomPath IS NOT NULL
    BEGIN
        -- Usar la ruta personalizada si se proporciona
        SET @BackupPath = @CustomPath;
    END
    ELSE
    BEGIN
        -- Usar la ruta por defecto si no se proporciona una ruta personalizada
        SET @BackupPath = 'C:\Backups\' + @DatabaseName + '\';

        -- Crear la estructura de carpetas si no existe
        DECLARE @FullPath NVARCHAR(512) = @BackupPath + 'Full\';
        DECLARE @DiffPath NVARCHAR(512) = @BackupPath + 'Differential\';
        DECLARE @LogPath NVARCHAR(512) = @BackupPath + 'Log\';

        EXEC xp_create_subdir @FullPath;
        EXEC xp_create_subdir @DiffPath;
        EXEC xp_create_subdir @LogPath;

        -- Asignar la ruta correspondiente al tipo de backup
        IF @BackupType = 'FULL'
            SET @BackupPath = @FullPath;
        ELSE IF @BackupType = 'DIFFERENTIAL'
            SET @BackupPath = @DiffPath;
        ELSE IF @BackupType = 'LOG'
            SET @BackupPath = @LogPath;
    END

    -- Generar el nombre del archivo de backup
    DECLARE @FileName NVARCHAR(512);

    IF @BackupName IS NOT NULL
    BEGIN
        -- Si el usuario proporciona un nombre de backup, concatenarlo con el formato predeterminado
        SET @FileName = @BackupPath + @BackupName + '_' + @DatabaseName + '_' + @BackupType + '_' + REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR, GETDATE(), 120), ':', ''), '-', ''), ' ', '_') + '.bak';
    END
    ELSE
    BEGIN
        -- Si no se proporciona un nombre de backup, usar el formato predeterminado
        SET @FileName = @BackupPath + @DatabaseName + '_' + @BackupType + '_' + REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR, GETDATE(), 120), ':', ''), '-', ''), ' ', '_') + '.bak';
    END

    -- Generar el nombre descriptivo del backup
    DECLARE @BackupDescription NVARCHAR(256);

    IF @BackupName IS NOT NULL
    BEGIN
        SET @BackupDescription = @BackupName + '_' + @DatabaseName + '_' + @BackupType + '_Backup';
    END
    ELSE
    BEGIN
        SET @BackupDescription = @DatabaseName + '_' + @BackupType + '_Backup';
    END

    -- Ejecutar el backup según el tipo
    IF @BackupType = 'FULL'
    BEGIN
        BACKUP DATABASE @DatabaseName
        TO DISK = @FileName
        WITH INIT, NAME = @BackupDescription, STATS = 10;
    END
    ELSE IF @BackupType = 'DIFFERENTIAL'
    BEGIN
        BACKUP DATABASE @DatabaseName
        TO DISK = @FileName
        WITH DIFFERENTIAL, INIT, NAME = @BackupDescription, STATS = 10;
    END
    ELSE IF @BackupType = 'LOG'
    BEGIN
        BACKUP LOG @DatabaseName
        TO DISK = @FileName
        WITH INIT, NAME = @BackupDescription, STATS = 10;
    END
    ELSE
    BEGIN
        RAISERROR('Tipo de backup no válido. Use FULL, DIFFERENTIAL o LOG.', 16, 1);
        RETURN;
    END

    PRINT 'Backup completado exitosamente en: ' + @FileName;
END
GO
```
---
### Interfaz Web

#### **Elementos Gráficos**
1. **Pantalla Principal**:
    - Información general sobre la estrategia de respaldo.
    - Botón para ejecutar un respaldo manualmente.

2. **Gestión de Backups**:
    - Formulario para seleccionar la base de datos y el tipo de backup.
    - Listado de backups generados con opción de descarga.

3. **Configuración**:
    - Configuración de rutas de almacenamiento para Windows Server y Docker.

---

### **Conclusión**
Este proyecto permite una gestión eficiente y automatizada de backups en SQL Server, garantizando la disponibilidad y recuperación de datos en caso de desastre. La implementación en dos entornos (Windows Server y Docker) asegura flexibilidad y adaptabilidad a diferentes infraestructuras. La interfaz web facilita la interacción con el sistema, haciendo que la gestión de backups sea accesible y sencilla.
