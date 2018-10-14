# Call Management Protocol
This document details the protocol used for communication between the ControllingFunction and MediaController/FloorController for managing calls

## Design Principles
* Binary - to reduce packet size and increase parsing performance
* UDP - for less packet overhead, control over retries, can no connection overheads.

## Packets

### Register Media Controller
Sent to the Controlling Function to register as a media controller
* Packet Type 0 (byte)
* Request ID (uint32)
* UDP Control Port (ushort)
* UDP Media Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Register Floor Controller
Sent to the Controlling Function to register as a floor controller
* Packet Type 1 (byte)
* Request ID (uint32)
* UDP Port (ushort)
* Floor Control Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Start Call
Tells the MediaController or Floor Controller to start managing a call
* Packet Type 2 (byte)
* Request ID (uint32)
* Call ID (uint16)
* Group ID (uint16)

### Ack
Acknoledgement to one of the other message types, all messages without specified responses must be acknowledged
* Packet Type 3 (byte)
* Request ID (uint32)

### Get Groups File Request
* Packet Type 4 (byte)
* Request ID (uint32)

### Get Group File Request
* Packet Type 5 (byte)
* Request ID (uint32)

### File Manifest Response
* Packet Type 6 (byte)
* Request ID (uint32)
* Number of Parts (uint16)
* File ID (uint16)

### File Part Request
* Packet Type 7 (byte)
* Request ID (uint32)
* File Id (uint16)
* Part Number (uint16)

### File Part Response
* Packet Type 8 (byte)
* Request ID (uint32) - (File ID and Part Number are not included becuase they can be infered from the Request ID)
* Payload

### Groups File Payload
* Group ID (uint16)
* ^ repeated for each group

### Group File Payload
* User ID (uint32)
* EndPoint (6 bytes)
* ^ repeat for each user

