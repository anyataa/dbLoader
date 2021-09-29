<p align="center">DBLoader - Oracle to SQL Server</p>


<p align="center">
  <a href="#features">Features</a> •
  <a href="https://www.linkedin.com/in/anya-tamara-akbar-74555514a/">Linkedin</a> •
  <a href="https://github.com/anyataa/AnyaTamara-Apptest/issues">Issues</a> •
  <a href="#license">License</a>
</p>

## Overview

This DB Loader worker allows you to map the Oracle Table to SQL Server based on the Configuration Table. The data from Oracle Table will not added twice as long as the Oracle Table has Insert_Time that can be used by the Parameter Table as the last point that will be used as a parameter to grab all the data that haven't grabbed previously. 

## Getting Started

Read more about how to use the DbLoader on  [website](https://solar-taxi-ef2.notion.site/DBLOADER-a7c5cfd9f4c14a0b9e0d144345d372a6) for the documentation and table structure for Parameter Table and Configuration Table.  

To run locally:  
**1. Change the oracle db connection to your particular oracle db**

`"ConnectionStrings": {"OracleDBConnection": "Your Oracle Connection Here"}`  

**2.  Change the sql server connection to your particular oracle db**  

` "ConnectionStrings": {"SqlServerDBConnection": "Your SQL Server Connection Here" }`

**3. Add source table : Oracle**

`"SourceDB1": "TABLE_TRANSACTION"`

**4. Add destination table : Sql Server**

`"DestinatitonDB1": "TABLE_TRANSACTION_LOADED"`

**5. Total DB and BU**

` "TotalDB": 2,"BU1": "DBLOADER",` Please note that total of table you want to load must be the same in Total DB. If you have 2 source table, then TotalDB is 2. The BU1 is a unique key to match the configuration for your source table and destination table. See the reccomended table structure in the documentation for knowing about BU more.

# Features
* Load data from Oracle to SQL Server
* Add as much as table you want (however please match the number of column and its type for both Oracle and SQL server)
* Customize the interval for each loading activity (in seconds)
* Customize the name of configuration table and parameter table (However changing the column structure is probihited, if you do so please rewrite the SQL select and update methods for some function. Hence, it is reccomended for not doing it unless you willingly to do edit the code.)
* Limit the data that fetched from Oracle for being loaded to SQL Server per loading prosess to avoid overflow.

# License
[MIT](https://tldrlegal.com/license/mit-license)

## Report Bug
If you find any bug or problem during using DbLoader feel free to reach out to me : anyatmr@gmail.com
