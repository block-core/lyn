using System.Collections;
using System.Collections.Generic;

namespace Lyn.Protocol.Tests.Bolt3
{
    public class Bolt3PerCommitmentStorageTestVectors : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                "insert_secret correct sequence",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (true, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                    (true, "0xc65716add7aa98ba7acb236352d665cab17345fe45b55fb879ff80e6bd0c41dd"),
                    (true, "0x969660042a28f32d9be17344e09374b379962d03db1574df5a8a5a47e19ce3f2"),
                    (true, "0xa5a64476122ca0925fb344bdc1854c1c0a59fc614298e50a33e331980a220f32"),
                    (true, "0x05cde6323d949933f7f7b78776bcc1ea6d9b31447732e3802e1f7ac44b650e17"),
                }
            };

            yield return new object[]
            {
                "insert_secret #1 incorrect",
                new object[]
                {
                    (true, "0x02a40c85b6f28da08dfdbe0926c53fab2de6d28c10301f8f7c4073d5e42e3148"),
                    (false, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                }
            };

            yield return new object[]
            {
                "insert_secret #2 incorrect (#1 derived from incorrect)",
                new object[]
                {
                    (true, "0x02a40c85b6f28da08dfdbe0926c53fab2de6d28c10301f8f7c4073d5e42e3148"),
                    (true, "0xdddc3a8d14fddf2b68fa8c7fbad2748274937479dd0f8930d5ebb4ab6bd866a3"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (false, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                }
            };

            yield return new object[]
            {
                "insert_secret #3 incorrect",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0xc51a18b13e8527e579ec56365482c62f180b7d5760b46e9477dae59e87ed423a"),
                    (false, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                }
            };

            yield return new object[]
            {
                "insert_secret #4 incorrect (1,2,3 derived from incorrect)",
                new object[]
                {
                    (true, "0x02a40c85b6f28da08dfdbe0926c53fab2de6d28c10301f8f7c4073d5e42e3148"),
                    (true, "0xdddc3a8d14fddf2b68fa8c7fbad2748274937479dd0f8930d5ebb4ab6bd866a3"),
                    (true, "0xc51a18b13e8527e579ec56365482c62f180b7d5760b46e9477dae59e87ed423a"),
                    (true, "0xba65d7b0ef55a3ba300d4e87af29868f394f8f138d78a7011669c79b37b936f4"),
                    (true, "0xc65716add7aa98ba7acb236352d665cab17345fe45b55fb879ff80e6bd0c41dd"),
                    (true, "0x969660042a28f32d9be17344e09374b379962d03db1574df5a8a5a47e19ce3f2"),
                    (true, "0xa5a64476122ca0925fb344bdc1854c1c0a59fc614298e50a33e331980a220f32"),
                    (false, "0x05cde6323d949933f7f7b78776bcc1ea6d9b31447732e3802e1f7ac44b650e17"),
                }
            };

            yield return new object[]
            {
                "insert_secret #5 incorrect",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (true, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                    (true, "0x631373ad5f9ef654bb3dade742d09504c567edd24320d2fcd68e3cc47e2ff6a6"),
                    (false, "0x969660042a28f32d9be17344e09374b379962d03db1574df5a8a5a47e19ce3f2"),
                }
            };

            yield return new object[]
            {
                "insert_secret #6 incorrect (5 derived from incorrect)",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (true, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                    (true, "0x631373ad5f9ef654bb3dade742d09504c567edd24320d2fcd68e3cc47e2ff6a6"),
                    (true, "0xb7e76a83668bde38b373970155c868a653304308f9896692f904a23731224bb1"),
                    (true, "0xa5a64476122ca0925fb344bdc1854c1c0a59fc614298e50a33e331980a220f32"),
                    (false, "0x05cde6323d949933f7f7b78776bcc1ea6d9b31447732e3802e1f7ac44b650e17"),
                }
            };

            yield return new object[]
            {
                "insert_secret #7 incorrect",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (true, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                    (true, "0xc65716add7aa98ba7acb236352d665cab17345fe45b55fb879ff80e6bd0c41dd"),
                    (true, "0x969660042a28f32d9be17344e09374b379962d03db1574df5a8a5a47e19ce3f2"),
                    (true, "0xe7971de736e01da8ed58b94c2fc216cb1dca9e326f3a96e7194fe8ea8af6c0a3"),
                    (false, "0x05cde6323d949933f7f7b78776bcc1ea6d9b31447732e3802e1f7ac44b650e17"),
                }
            };

            yield return new object[]
            {
                "insert_secret #8 incorrect",
                new object[]
                {
                    (true, "0x7cc854b54e3e0dcdb010d7a3fee464a9687be6e8db3be6854c475621e007a5dc"),
                    (true, "0xc7518c8ae4660ed02894df8976fa1a3659c1a8b4b5bec0c4b872abeba4cb8964"),
                    (true, "0x2273e227a5b7449b6e70f1fb4652864038b1cbf9cd7c043a7d6456b7fc275ad8"),
                    (true, "0x27cddaa5624534cb6cb9d7da077cf2b22ab21e9b506fd4998a51d54502e99116"),
                    (true, "0xc65716add7aa98ba7acb236352d665cab17345fe45b55fb879ff80e6bd0c41dd"),
                    (true, "0x969660042a28f32d9be17344e09374b379962d03db1574df5a8a5a47e19ce3f2"),
                    (true, "0xa5a64476122ca0925fb344bdc1854c1c0a59fc614298e50a33e331980a220f32"),
                    (false, "0xa7efbc61aac46d34f77778bac22c8a20c6a46ca460addc49009bda875ec88fa4"),
                }
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}