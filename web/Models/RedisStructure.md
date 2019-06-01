# NextUserId
* Description - Holds the next User ID to use
* Redis Type - string
* Logical Type - int

# IdByUsername:{username}
* Description - Lookup User ID by username
* Redis Type - string
* Logical Type - int

# Users:{id}
* Description - Lookup User json by User ID
* Redis Type - string
* Logical Type - json (User)

# Users
* Description - sorted set of user ids (by name) for paging
* Redis Type - sorted set
* Logical Type - int

# UsersCredentials:{username}
* Description - UserCredentials by username
* Redis Type - string
* Logical Type - json of UserCredentials (UserName, PasswordHash, Roles)

