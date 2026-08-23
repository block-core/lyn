# Lyn - Lightning Your .NET
Adopt the Lightning protocol in your own software to enable use-cases beyond blockchains.

Lightning implementation in C#. Lyn is an open source development project working to implement the Basis of Lightning Technology (BOLT)

The following bullet list represents the current status of Lyn:

  - [X] BOLT 1: Base Protocol
  - [X] BOLT 2: Peer Protocol for Channel Management
  - [X] BOLT 3: Bitcoin Transaction and Script Formats
  - [X] BOLT 4: Onion Routing Protocol (sphinx packet construction, processing and error return path)
  - [ ] BOLT 5: Recommendations for On-chain Transaction Handling
  - [X] BOLT 7: P2P Node and Channel Discovery (including gossip queries)
  - [X] BOLT 8: Encrypted and Authenticated Transport
  - [X] BOLT 9: Assigned Feature Flags (all currently assigned features)
  - [X] BOLT 10: DNS Bootstrap and Assisted Node Location
  - [X] BOLT 11: Invoice Protocol for Lightning Payments

## Notes on the current implementation

- **BOLT 2** covers the full message surface: channel establishment, `channel_reestablish`,
  normal operations (`update_add_htlc`, `update_fulfill_htlc`, `update_fail_htlc`,
  `update_fail_malformed_htlc`, `update_fee`, `commitment_signed`, `revoke_and_ack`)
  and cooperative closing (`shutdown`, `closing_signed`). All messages have wire format
  serializers and inbound processing services with state machine and validation logic.
- **BOLT 4** implements the variable length (`var_onion_optin`) TLV onion payload format,
  sphinx packet construction/processing, blinding of ephemeral keys and the encrypted
  error return path, verified against the official test vectors.
- **BOLT 7** now includes the gossip query messages (`query_channel_range`,
  `reply_channel_range`, `query_short_channel_ids`, `reply_short_channel_ids_end`)
  including the encoded short channel id formats.
- **BOLT 11** supports parsing and creating invoices with payment secrets (`s`), feature
  bits, fallback on-chain addresses, route hints, payment metadata and public key
  recovery, verified against the official test vectors.
- **BOLT 10** provides a minimal UDP based DNS client for `A`, `AAAA` and `SRV` queries
  and decodes the bech32 virtual hostnames returned by lightning DNS seeds.

## Funding

The team behind Lyn is currently looking for additional funding to continue the development of our Lightning-implementation.

Read more on our website: https://lyn.blockcore.net/
