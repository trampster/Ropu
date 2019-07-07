# NextUserId
* Description - Holds the next User ID to use
* Redis Type - string
* Logical Type - int

# IdByEmail:{email}
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

# Image:{imagehash}
* Description - Image lookup by SHA256 hash
* Redis Type - string
* Logical Type - byte[] of image