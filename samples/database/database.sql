create table Category(
	CategoryId int,
	CategoryName varchar(15) not null,
	CONSTRAINT PK_Category PRIMARY KEY CLUSTERED (CategoryId)  
)
go

create table Customer(
	CustomerId varchar(5),
	CompanyName varchar(40) not null,
	ContactName varchar(30),
	City varchar(15),
	Country varchar(15) ,
	CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (CustomerId)  
)
go

create table CustomerSetting(
	CustomerId varchar(5),
	BirthDate varchar(5),
	Setting varchar(50) not null,
	CONSTRAINT PK_CustomerSetting PRIMARY KEY CLUSTERED (CustomerId) ,
	constraint FK_CustomerSetting_Customer foreign key(CustomerId) references Customer(CustomerId) -- 1...1
)
go

create table Employee(
	EmployeeId int ,
	LastName varchar(20) not null,
	FirstName varchar(20) not null,
	BirthDate datetime,
	HireDate datetime,
	City varchar(15),
	Country varchar(15),
	CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED (EmployeeId) ,
)
go

create table Territory(
	TerritoryId varchar(20) ,
	TerritoryDescription varchar(50) not null,
	CONSTRAINT PK_Territory PRIMARY KEY CLUSTERED (TerritoryId) 
)
go

create table EmployeeTerritories(
	EmployeeId int,
	TerritoryId varchar(20),
	constraint PK_EmployeeTerritories primary key clustered(EmployeeId,TerritoryId),
	constraint FK_EmployeeTerritories_Employee_EmployeeId foreign key(EmployeeId) references Employee(EmployeeId),	--*...*
	constraint FK_EmployeeTerritories_Territory_TerritoryId foreign key(TerritoryId) references Territory(TerritoryId)	--*...*
)
go

create table [Order](
	OrderId int,
	CustomerId varchar(5) not null,
	OrderDate datetime,
	ShippedDate datetime,
	ShipVia int,
	Freight money default(0),
	constraint PK_Order primary key clustered(OrderId),
	constraint FK_Order_Customers foreign key(CustomerId) references Customer(CustomerId) --1...*
)
go

create table Product(
	ProductId int,
	ProductName varchar(5) not null,
	CategoryId int,
	UnitPrice money,
	Discontinued bit,
	[RowVersion] timestamp,
	Freight money default(0),
	constraint PK_Product primary key clustered(ProductId),
	constraint FK_Products_Category foreign key(CategoryId) references Category(CategoryId)--1...*
)
go

create table OrderDetail(
	OrderDetailId int,
	OrderId int,
	ProductId int,
	UnitPrice money default(0),
	Quantity smallint default(1),
	Discount float,
	constraint PK_OrderDetail primary key clustered(OrderDetailId),
	constraint FK_OrderDetails_Order foreign key(OrderId) references [Order](OrderId),	--1...*
	constraint FK_OrderDetails_Product foreign key(ProductId) references Product(ProductId),--1...*
)
go
