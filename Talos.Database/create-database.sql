CREATE DATABASE Talos;
USE Talos;

CREATE TABLE package(
id int primary key auto_increment,
name varchar(255),
short_name varchar(100),
package_manager varchar(150),
create_at int
);

CREATE TABLE user(
id int primary key auto_increment,
user_name varchar(100),
email varchar(150),
tier enum('free', 'bussines', 'personal'),
private_template_limit varchar(50)
);

CREATE TABLE package_version(
id int primary key auto_increment,
package_id int,
foreign key (package_id) references package (id),
version varchar(50),
release_date int,
is_deprecated bool
);

CREATE TABLE compatibility(
id int primary key auto_increment,
package_version_id int,
foreign key (package_version_id) references package_version (id),
compatible_versions_range varchar(100)
);

CREATE TABLE template(
id int primary key auto_increment,
user_id int,
foreign key (user_id) references user (id),
template_name varchar(150),
slug varchar(255),
is_public bool,
license_type varchar(50)
);

CREATE TABLE template_dependencies(
id int primary key auto_increment,
template_id int,
foreign key (template_id) references template (id),
package_id int,
foreign key (package_id) references package (id),
version_constraint varchar(100)
);