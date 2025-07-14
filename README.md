18practical
На чем написано: C#

Что это?  
18 практика с 2го курса, в задании которой было сделать создание элементов и их сортировку. Из шутки превратилось в временную локальную сеть колледжа с функционалом создания файлов/тегов, загрузки их в БД и просмотра с любого другого пк. Ничего больше там нет, кроме просмотра файлов, загрузки данных для файлов и редактирования цвета тегов.

Скриншоты в папке "Скриншотики" в корне проекта. Положил так, на случай если посмотреть хочется, а запускать нет

Как запустить?  
Файлик 18practical.sln в корне проекта для запуска проекта, или какой то с .cs для просмотра кода. Или просто bin/Debug/18practical.exe для быстрого запуска, но без базы данных не заработает

SQL запрос для восстановления базы данных
CREATE DATABASE Redas;
GO
USE Redas;
GO
CREATE TABLE Table_Items (
    item_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    extension NVARCHAR(50),
    image VARBINARY(MAX),
    filepath VARBINARY(MAX),
    description NVARCHAR(MAX)
);
CREATE TABLE Table_Tags (
    tag_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL UNIQUE,
    color NVARCHAR(7) DEFAULT '#400040'
);
CREATE TABLE Table_connection_item_tag (
    item_id INT NOT NULL,
    tag_id INT NOT NULL,
    PRIMARY KEY (item_id, tag_id),
    FOREIGN KEY (item_id) REFERENCES Table_Items(item_id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES Table_Tags(tag_id) ON DELETE CASCADE
);