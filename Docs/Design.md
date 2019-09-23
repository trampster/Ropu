# High Performance PTToC Proposal

## Problem statement
The performance and scalability of the existing design is questionable with the following potential issues identified:

### Slow Group Call Setup Time
The group call setup is slow it takes seconds to send out all the invites to a few thousand units and will be even longer if we wait for replies. Our LMR clients are used to almost instant group calls and having to wait will not be a positive user experience for them.

### Max units in a call
We have yet to test this but we need to be able to stream media to thousands of units. It is therefore not clear if a single Conference Controller will be able to meet this requirement.

### Design issues with SIP/MCPTT for scaling
*SIP*
SIP although an industry standard is horribly inefficient. It is a text based protocol which was not designed with any consideration for performance of ether parsing, writing or network usage. The invites are so bloated that with the addition of the bits required for MCPTT they no longer fit in standard UDP packets and need either fragmentation support or the use of TCP.

When a group call is created each and every participant needs to be invited and have media and ports negotiated. This is what causes the large delays in group call setup.

*RTP*
RTP has unique identifiers for each stream, this means that a Conference Controller can’t send the same packet to all participants in a call, each packet needs to be specifically created for that participant, this requires additional CPU and memory allocations which would otherwise not be required. Without this the Conference Control could take the incoming packet from the user who has the floor and without modification send it to all call participants. Such a system would be limited only by the bandwidth of the connection it is using.

## Proposed solution
Controller Protocol
Replace both the SIP and Floor Control protocols with a single control plane protocol, With the following properties:
Binary rather than text (for size and speed of parsing)
No variable length fields for messages that will be sent to all participants  (for speed of parsing)
No unique identifiers, (the same packet can be sent to all participants without modification)
Media and ports will be negotiated at registration time, not all call setup.
Presence will be keeped up to date via control plane messages so that we already know who can receive a call when it starts.
When group call starts, media will immediately be streamed to all participants. The initiator will implicately granted the floor (no floor taken message sent). Call start will be sent concurrently with media but clients should not wait for them to unmute.
The exact same floor taken message will be sent to all participants including the requester.
Floor denied will be sent only to the requester.

The client will share a port for the control, media and floor packets. This is so we only need to keep one mapping alive through the NAT rather than three. For this reason the packet type field needs to be unique accross all three protocols.

### Controlling Function Protocol
#### Registration
* Packet Type 0 (byte)
* User ID (uint32)

#### Registration Response
* Packet Type 1 (byte)
* User ID (uint32)
* Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, this is required so the server doesn’t have to transcode, which is an expensive operation)
* Bitrate (uint16)

#### Call Ended 
units receiving this packet should not stop playing out media they receive, this should only be used to update the UI)
* Packet Type 2
* Group ID (uint16)

#### Start Group Call
* Packet Type 3
* Group ID ((uint16))
* User ID (uint32)

### Call Start Failed
* Packet Type 5
* User ID (uint32)
* Reason (byte) 0 = insufficient resources, 255 = other reason

### Floor Control Protocol
#### Floor Denied
* Packet Type 6
* Group ID (uint16)
* User ID (uint32)

#### Floor Taken
* Packet Type 7
* Group ID (uint16)
* User ID (uint32) 

#### Floor Idle
* Packet Type 8
* Group ID (uint16)

#### Floor Released
* Packet Type 9
* Group ID (uint16)
* User ID (uint32) 

#### Floor Request
* Packet Type 10
* Group ID (uint16)
* User ID (uint32) 

### Media Plane Protocol
Clients should play out everything they receive on the Media Plane, regardless of the call state. This way if the control messages and media packets arrive unsynchronized the client won't miss audio.
Likewise when starting a call, the client should start streaming the media to the server as soon as they have received the Start Call Response before receiving floor granted. The call initiator is implicitly granted the floor.

#### Media Packet Individual Call
* Packet Type 11 (byte)
* Group ID (uint16)
* Key ID (uint16) - 0 means no encryption
* Payload

### Media Packet Group Call
* Packet Type (12 from Client, 13 from Serving Node) (byte)
* Group Id (uint16)
* Sequence Number (uint16)
* User ID (uint32)
* Key ID (uint16) - 0 means no encryption
* Payload

### Heartbeat
* Packet Type 14 (byte)
* User ID (uint32)

### Heartbeat Resposne
* Packet Type 15 (byte)

### Not Registered
Sent when the serving node receives a packet from a client that isn't registered
* Packet Type 16 (byte)

### Deregister
* Packet Type 17 (byte)
* User ID (uint32)

### Deregister Rsponse
* Packet Type 18 (byte)


## Encryption
All packets are encrypted.
Keys are created and distributed via the web. There are always three keys for each group, user or service, todays, yesterdays and tomorrows. This allows for smooth handover of keys on day changes, where clocks by not be perfectly aligned. Sending should always be with todays key (according to the senders understanding of time) but receiving should accept any of the three keys. The Key ID increment for each day and wrap round to zero in the sequence, 0, 1, 2, 0, 1, 2... The Web will inform the user which key is for which day, as follows:
{
    "KeyId":1,
    "Date":"2019-03-20",
    "KeyMaterial":"95888a5d1ef426af363222edb8328328f3bbd1b6f7d33d8708b83a41dc47e3af",
    "IV":"407fb5acbaf15fe3a43bc6411bdd6e9f"
}
{
    "KeyId":2,
    "Date":"2019-03-21",
    "KeyMaterial":"21b4835b0fe636d3a9c1bd8b129f2af314e917dc7fdee26e1c1e7e215bf4453c",
    "IV":"c2f490ff1d1a10079b9b1ceb1cd47aba"
}
{
    "KeyId":0,
    "Date":"2019-03-22",
    "KeyMaterial":"dae9d8452e5e8dccb66a76f9b329d67273c5564c565432f5244f914c1985495a"
    "IV":"73ca34c4a034c0e7f4e761f8f696c2ae"
}
Dates are specified in UTC time.
* Type (0 = group, 1 = user, 2 = service) (2 bits)
* KeyId (2 bits) - 
* Reserved (5 bits) - 
* SourceId (4 bytes) - groupId, userId or serviceId depending on type
* Sequence Number (4 bytes)
* Encrypted payload (16 bytes)