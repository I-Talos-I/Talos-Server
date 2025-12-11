# Talos 

## Creación del esquema DB en MySQL 
### Database :
 
```bash
CREATE DATABASE Talos;
```
### Tablas :

- **package**
    ```bash
    CREATE TABLE package(
    id int primary key auto_increment,
    name varchar(255),
    short_name varchar(100),
    package_manager varchar(150),
    create_at int
    );
    ```
- **user**

    ```bash
    CREATE TABLE user(
    id int primary key auto_increment,
    user_name varchar(100),
    email varchar(150),
    tier enum('free', 'bussines', 'personal'),
    private_template_limit varchar(50)
    );
    ```
- **package_version**

    ```bash
    CREATE TABLE package_version(
    id int primary key auto_increment,
    package_id int,
    foreign key (package_id) references package (id),
    version varchar(50),
    release_date int,
    is_deprecated bool
    );
    ```
- **compatibility**

    ```bash
    CREATE TABLE compatibility(
    id int primary key auto_increment,
    package_version_id int,
    foreign key (package_version_id) references package_version (id),
    compatible_versions_range varchar(100)
    );
    ```
- **template**

    ```bash
    CREATE TABLE template(
    id int primary key auto_increment,
    user_id int,
    foreign key (user_id) references user (id),
    template_name varchar(150),
    slug varchar(255),
    is_public bool,
    license_type varchar(50)
    );
    ```
- **template_dependencies**

    ```bash
    CREATE TABLE template_dependencies(
    id int primary key auto_increment,
    template_id int,
    foreign key (template_id) references template (id),
    package_id int,
    foreign key (package_id) references package (id),
    version_constraint varchar(100)
    );
    ```

### Relaciones :

- Relación de la tabla `package_version`(**N**) con lo tabla `package`(**1**)
    ```bash
    foreign key (package_id) references package (id),
    ```
    ---
 
- Relación de la tabla `compatibility`(**N**) con la tabla `package_version`(**1**) 
    ```bash
    foreign key (package_version_id) references package_version (id)
    ```
    ---

- Relación de la tabla `template`(**N**) con la tabla `user`(**1**)
    ```bash
    foreign key (user_id) references user (id)
    ```
    ---

- Relación de la tabla `template_dependencies`(**N**) con la tabla `template`(**1**)  
    ```bash
    foreign key (template_id) references template (id)
    ```
    ---

- Relación de la tabla `template_dependencies`(**N**) con la tabla `package`(**1**) 
    ```bash
    foreign key (package_id) references package (id)
    ```
    ---

### Indice :

- Implementación de indice en la tabla `package`en la columna `id`
    ```bash
    ALTER TABLE package ADD INDEX (id);
    ```