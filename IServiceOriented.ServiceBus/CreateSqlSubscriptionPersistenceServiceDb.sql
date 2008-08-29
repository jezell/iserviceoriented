create table listener (id uniqueidentifier primary key, name nvarchar(max), address nvarchar(max) not null, contract_type nvarchar(max) not null, configuration_name nvarchar(max) not null, listener_type nvarchar(max) not null)
go

create table subscription (id uniqueidentifier primary key, filter_type nvarchar(max), filter_data nvarchar(max), name nvarchar(max), address nvarchar(max) not null, contract_type nvarchar(max) not null, configuration_name nvarchar(max) not null, dispatcher_type nvarchar(max))
go

create proc sp_listener_create (@id as uniqueidentifier, @name as nvarchar(max), @address as nvarchar(max), @contract_type as nvarchar(max), @configuration_name as nvarchar(max), @listener_type as nvarchar(max))
as
insert into listener (id, name, address, contract_type, configuration_name, listener_type) values (@id, @name, @address, @contract_type, @configuration_name, @listener_type)

go

create proc sp_listener_delete (@id as uniqueidentifier)
as
delete from listener where id = @id

go

create proc sp_subscription_create (@id as uniqueidentifier, @name as nvarchar(max), @address as nvarchar(max), @contract_type as nvarchar(max), @configuration_name as nvarchar(max), @filter_type as nvarchar(max), @filter_data as nvarchar(max), @dispatcher_type as nvarchar(max))
as
insert into subscription (id, name, address, contract_type, configuration_name, filter_type, filter_data, dispatcher_type) values (@id, @name, @address, @contract_type, @configuration_name, @filter_type, @filter_data, @dispatcher_type)

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

