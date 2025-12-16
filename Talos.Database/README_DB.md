# Talos Database Documentation

## Creación del esquema DB en MySQL

### Database

```sql
CREATE DATABASE Talos;
```

### Tablas

- **Users**
    ```sql
    CREATE TABLE Users (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        Username VARCHAR(255) NOT NULL,
        Email VARCHAR(255) NOT NULL,
        PasswordHash LONGTEXT NOT NULL,
        Role VARCHAR(50) DEFAULT 'user',
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
    );
    ```

- **Templates**
    ```sql
    CREATE TABLE Templates (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        UserId INT,
        Name VARCHAR(100) NOT NULL,
        Description LONGTEXT,
        Slug VARCHAR(120) NOT NULL UNIQUE,
        IsPublic BOOLEAN DEFAULT FALSE,
        LicenseType VARCHAR(50) DEFAULT 'MIT',
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );
    ```

- **PackageManagers**
    ```sql
    CREATE TABLE PackageManagers (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        Name LONGTEXT
    );
    ```

- **Packages**
    ```sql
    CREATE TABLE Packages (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        Name LONGTEXT,
        ShortName LONGTEXT,
        RepositoryUrl LONGTEXT,
        OfficialDocumentationUrl LONGTEXT,
        LastScrapedAt DATETIME,
        IsActive BOOLEAN,
        CreateAt DATETIME,
        UpdateAt DATETIME,
        PackageManagerId INT,
        FOREIGN KEY (PackageManagerId) REFERENCES PackageManagers(Id)
    );
    ```

- **PackageVersions**
    ```sql
    CREATE TABLE PackageVersions (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        PackageId INT,
        Version LONGTEXT,
        ReleaseDate DATETIME,
        IsDeprecated BOOLEAN,
        DeprecationMessage LONGTEXT,
        DownloadUrl LONGTEXT,
        ReleaseNotesUrl LONGTEXT,
        CreateAt DATETIME,
        FOREIGN KEY (PackageId) REFERENCES Packages(Id)
    );
    ```

- **Compatibilities**
    ```sql
    CREATE TABLE Compatibilities (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        SourcePackageVersionId INT,
        TargetPackageVersionId INT,
        TargetVersionConstraint LONGTEXT,
        CompatibilityType LONGTEXT,
        CompatibilityScore INT,
        ConfidenceLevel LONGTEXT,
        DetectedBy LONGTEXT,
        DetectionDate DATETIME,
        Notes LONGTEXT,
        IsActive BOOLEAN,
        FOREIGN KEY (SourcePackageVersionId) REFERENCES PackageVersions(Id) ON DELETE RESTRICT,
        FOREIGN KEY (TargetPackageVersionId) REFERENCES PackageVersions(Id) ON DELETE RESTRICT
    );
    ```

- **TemplateDependencies**
    ```sql
    CREATE TABLE TemplateDependencies (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        TemplateId INT,
        Name VARCHAR(100) NOT NULL,
        FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE CASCADE
    );
    ```

- **DependencyVersions**
    ```sql
    CREATE TABLE DependencyVersions (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        TemplateDependencyId INT,
        Version VARCHAR(20) NOT NULL,
        FOREIGN KEY (TemplateDependencyId) REFERENCES TemplateDependencies(Id)
    );
    ```

- **DependencyCommands**
    ```sql
    CREATE TABLE DependencyCommands (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        TemplateDependencyId INT,
        OS INT,
        Command LONGTEXT NOT NULL,
        `Order` INT,
        FOREIGN KEY (TemplateDependencyId) REFERENCES TemplateDependencies(Id)
    );
    ```

- **Posts**
    ```sql
    CREATE TABLE Posts (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        Title LONGTEXT,
        Body LONGTEXT,
        Status LONGTEXT,
        CreatedAt DATETIME,
        UserId INT,
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
    ```

- **RefreshTokens**
    ```sql
    CREATE TABLE RefreshTokens (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        Token LONGTEXT,
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
        ExpiresAt DATETIME,
        IsRevoked BOOLEAN DEFAULT FALSE,
        UserId INT,
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
    ```

- **ApiKeys**
    ```sql
    CREATE TABLE ApiKeys (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        `Key` VARCHAR(128) NOT NULL,
        Owner VARCHAR(50) NOT NULL,
        Role VARCHAR(50) DEFAULT 'user',
        IsActive BOOLEAN DEFAULT TRUE,
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
        ExpiresAt DATETIME,
        Scope VARCHAR(200),
        UsageCount INT DEFAULT 0,
        MaxUsage INT
    );
    ```

- **ApiKeyAudits**
    ```sql
    CREATE TABLE ApiKeyAudits (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        ApiKeyId INT,
        Endpoint LONGTEXT,
        IP LONGTEXT,
        AccessedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (ApiKeyId) REFERENCES ApiKeys(Id)
    );
    ```

### Diagrama MER (Mermaid)

<img src="./MER_TALOS.png" alt="Diagrama de Arquitectura" width="600">

### Relaciones Clave

- **Users -> Templates**: Un usuario puede crear múltiples templates. Si el usuario se elimina, el `UserId` en templates se establece a NULL.
- **Templates -> TemplateDependencies**: Un template tiene múltiples dependencias. Si se borra el template, se borran sus dependencias (Cascade).
- **Packages -> PackageVersions**: Un paquete tiene múltiples versiones.
- **PackageVersions -> Compatibilities**: Las versiones de paquetes tienen relaciones de compatibilidad con otras versiones (Source/Target).