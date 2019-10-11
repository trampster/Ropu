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

# Services:{userId}
* Description - service info
* Redis Type - string
* Logical Type - json:
{
    "Type":"LoadBalander|ServingNode|CallController"
}

# Services
* Description - sorted set of services' userIds
* Redis Type - set
* Logical Type - int


# EncryptionKeys:{type}{userId|groupId}
Where type is 0, 1 (0 is group, 1 is user)
* Description - set of all encryption keys
* Redis Type - string
* Logical Type - json:
[
    {
        "KeyId":1,
        "Date":"2019-03-20",
        "KeyMaterial":"95888a5d1ef426af363222edb8328328f3bbd1b6f7d33d8708b83a41dc47e3af",
    }
    {
        "KeyId":2,
        "Date":"2019-03-21",
        "KeyMaterial":"21b4835b0fe636d3a9c1bd8b129f2af314e917dc7fdee26e1c1e7e215bf4453c",
    }
    {
        "KeyId":0,
        "Date":"2019-03-22",
        "KeyMaterial":"dae9d8452e5e8dccb66a76f9b329d67273c5564c565432f5244f914c1985495a"
    }
]



Questions we need to ask
* given this userid what are the keys
* given this groupid what are the keys

* Each user needs to get the load balancer keys, and it's service node keys
* Serving nodes need all the other services keys (load balancer, serving nodes, call controllers)
* Serving nodes need to know the keys of the users registered with it