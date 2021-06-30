namespace Lyn.Protocol.Bolt3.Types
{
    public enum HtlcState
    {
        /* When we add a new htlc, it goes in this order. */
        SentAddHtlc,
        SentAddCommit,
        RcvdAddRevocation,
        RcvdAddAckCommit,
        SentAddAckRevocation,

        /* When they remove an HTLC, it goes from SENT_ADD_ACK_REVOCATION: */
        RcvdRemoveHtlc,
        RcvdRemoveCommit,
        SentRemoveRevocation,
        SentRemoveAckCommit,
        RcvdRemoveAckRevocation,

        /* When they add a new htlc, it goes in this order. */
        RcvdAddHtlc,
        RcvdAddCommit,
        SentAddRevocation,
        SentAddAckCommit,
        RcvdAddAckRevocation,

        /* When we remove an HTLC, it goes from RCVD_ADD_ACK_REVOCATION: */
        SentRemoveHtlc,
        SentRemoveCommit,
        RcvdRemoveRevocation,
        RcvdRemoveAckCommit,
        SentRemoveAckRevocation,

        HtlcStateInvalid
    };
}