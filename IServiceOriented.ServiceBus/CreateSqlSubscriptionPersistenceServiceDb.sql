create table listener (id uniqueidentifier primary key, name nvarchar(max), address nvarchar(max) not null, contract_type nvarchar(max) not null, configuration_name nvarchar(max) not null, listener_data varbinary(max) not null)
go

create table subscription (id uniqueidentifier primary key, filter_data varbinary(max), name nvarchar(max), address nvarchar(max) not null, contract_type nvarchar(max) not null, configuration_name nvarchar(max) not null, dispatcher_data varbinary(max))
go

create proc sp_listener_create (@id as uniqueidentifier, @name as nvarchar(max), @address as nvarchar(max), @contract_type as nvarchar(max), @configuration_name as nvarchar(max), @listener_data as varbinary(max))
as
insert into listener (id, name, address, contract_type, configuration_name, listener_data) values (@id, @name, @address, @contract_type, @configuration_name, @listener_data)

go

create proc sp_listener_delete (@id as uniqueidentifier)
as
delete from listener where id = @id

go

create proc sp_subscription_create (@id as uniqueidentifier, @name as nvarchar(max), @address as nvarchar(max), @contract_type as nvarchar(max), @configuration_name as nvarchar(max), @filter_data as varbinary(max), @dispatcher_data as varbinary(max))
as
insert into subscription (id, name, address, contract_type, configuration_name, filter_data, dispatcher_data) values (@id, @name, @address, @contract_type, @configuration_name, @filter_data, @dispatcher_data)

go

create proc sp_subscription_delete (@id as uniqueidentifier)
as
delete from subscription where id = @id

go

create proc sp_subscription_list 
as
select * from subscription

go

create proc sp_listener_list
as
select * from listener

