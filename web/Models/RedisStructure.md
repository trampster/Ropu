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



# NextGroupId
* Description - Holds the next Group ID to use
* Redis Type - string
* Logical Type - int

# Groups:{id}
* Description - Lookup Group json by Group ID
* Redis Type - string
* Logical Type - json (Group)

# GroupIdByName:{name}
* Description - Lookup Group ID by name
* Redis Type - string
* Logical Type - int

# Groups
* Description - sorted set of group ids (sorted by name) for paging
* Redis Type - sorted set
* Logical Type - int

# GroupMembers:{groupId}
* Description - set of group member ids (sorted by name) for paging
* Redis Type - sorted set
* Logical Type - int

# UsersGroups:{userId}
* Description - set of group ids (sorted by group name) for paging
* Redis Type - sorted set
* Logical Type - int