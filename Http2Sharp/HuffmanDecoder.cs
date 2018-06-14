using System;
using System.Diagnostics;
using System.Text;

namespace Http2Sharp
{
    internal static class HuffmanDecoder
    {
        private struct HuffmanState
        {
            public bool EndPoint;
            public int Left;
            public int Right;
        }

        private const int STATES_NEEDED = 513;

        private static readonly HuffmanState[] states = new HuffmanState[STATES_NEEDED];

        static HuffmanDecoder()
        {
            var i = 1;
            AddCode(0, 0x1ff8, 13, 0, ref i);
            AddCode(1, 0x7fffd8, 23, 0, ref i);
            AddCode(2, 0xfffffe2, 28, 0, ref i);
            AddCode(3, 0xfffffe3, 28, 0, ref i);
            AddCode(4, 0xfffffe4, 28, 0, ref i);
            AddCode(5, 0xfffffe5, 28, 0, ref i);
            AddCode(6, 0xfffffe6, 28, 0, ref i);
            AddCode(7, 0xfffffe7, 28, 0, ref i);
            AddCode(8, 0xfffffe8, 28, 0, ref i);
            AddCode(9, 0xffffea, 24, 0, ref i);
            AddCode(10, 0x3ffffffc, 30, 0, ref i);
            AddCode(11, 0xfffffe9, 28, 0, ref i);
            AddCode(12, 0xfffffea, 28, 0, ref i);
            AddCode(13, 0x3ffffffd, 30, 0, ref i);
            AddCode(14, 0xfffffeb, 28, 0, ref i);
            AddCode(15, 0xfffffec, 28, 0, ref i);
            AddCode(16, 0xfffffed, 28, 0, ref i);
            AddCode(17, 0xfffffee, 28, 0, ref i);
            AddCode(18, 0xfffffef, 28, 0, ref i);
            AddCode(19, 0xffffff0, 28, 0, ref i);
            AddCode(20, 0xffffff1, 28, 0, ref i);
            AddCode(21, 0xffffff2, 28, 0, ref i);
            AddCode(22, 0x3ffffffe, 30, 0, ref i);
            AddCode(23, 0xffffff3, 28, 0, ref i);
            AddCode(24, 0xffffff4, 28, 0, ref i);
            AddCode(25, 0xffffff5, 28, 0, ref i);
            AddCode(26, 0xffffff6, 28, 0, ref i);
            AddCode(27, 0xffffff7, 28, 0, ref i);
            AddCode(28, 0xffffff8, 28, 0, ref i);
            AddCode(29, 0xffffff9, 28, 0, ref i);
            AddCode(30, 0xffffffa, 28, 0, ref i);
            AddCode(31, 0xffffffb, 28, 0, ref i);
            AddCode(32, 0x14, 6, 0, ref i);
            AddCode(33, 0x3f8, 10, 0, ref i);
            AddCode(34, 0x3f9, 10, 0, ref i);
            AddCode(35, 0xffa, 12, 0, ref i);
            AddCode(36, 0x1ff9, 13, 0, ref i);
            AddCode(37, 0x15, 6, 0, ref i);
            AddCode(38, 0xf8, 8, 0, ref i);
            AddCode(39, 0x7fa, 11, 0, ref i);
            AddCode(40, 0x3fa, 10, 0, ref i);
            AddCode(41, 0x3fb, 10, 0, ref i);
            AddCode(42, 0xf9, 8, 0, ref i);
            AddCode(43, 0x7fb, 11, 0, ref i);
            AddCode(44, 0xfa, 8, 0, ref i);
            AddCode(45, 0x16, 6, 0, ref i);
            AddCode(46, 0x17, 6, 0, ref i);
            AddCode(47, 0x18, 6, 0, ref i);
            AddCode(48, 0x0, 5, 0, ref i);
            AddCode(49, 0x1, 5, 0, ref i);
            AddCode(50, 0x2, 5, 0, ref i);
            AddCode(51, 0x19, 6, 0, ref i);
            AddCode(52, 0x1a, 6, 0, ref i);
            AddCode(53, 0x1b, 6, 0, ref i);
            AddCode(54, 0x1c, 6, 0, ref i);
            AddCode(55, 0x1d, 6, 0, ref i);
            AddCode(56, 0x1e, 6, 0, ref i);
            AddCode(57, 0x1f, 6, 0, ref i);
            AddCode(58, 0x5c, 7, 0, ref i);
            AddCode(59, 0xfb, 8, 0, ref i);
            AddCode(60, 0x7ffc, 15, 0, ref i);
            AddCode(61, 0x20, 6, 0, ref i);
            AddCode(62, 0xffb, 12, 0, ref i);
            AddCode(63, 0x3fc, 10, 0, ref i);
            AddCode(64, 0x1ffa, 13, 0, ref i);
            AddCode(65, 0x21, 6, 0, ref i);
            AddCode(66, 0x5d, 7, 0, ref i);
            AddCode(67, 0x5e, 7, 0, ref i);
            AddCode(68, 0x5f, 7, 0, ref i);
            AddCode(69, 0x60, 7, 0, ref i);
            AddCode(70, 0x61, 7, 0, ref i);
            AddCode(71, 0x62, 7, 0, ref i);
            AddCode(72, 0x63, 7, 0, ref i);
            AddCode(73, 0x64, 7, 0, ref i);
            AddCode(74, 0x65, 7, 0, ref i);
            AddCode(75, 0x66, 7, 0, ref i);
            AddCode(76, 0x67, 7, 0, ref i);
            AddCode(77, 0x68, 7, 0, ref i);
            AddCode(78, 0x69, 7, 0, ref i);
            AddCode(79, 0x6a, 7, 0, ref i);
            AddCode(80, 0x6b, 7, 0, ref i);
            AddCode(81, 0x6c, 7, 0, ref i);
            AddCode(82, 0x6d, 7, 0, ref i);
            AddCode(83, 0x6e, 7, 0, ref i);
            AddCode(84, 0x6f, 7, 0, ref i);
            AddCode(85, 0x70, 7, 0, ref i);
            AddCode(86, 0x71, 7, 0, ref i);
            AddCode(87, 0x72, 7, 0, ref i);
            AddCode(88, 0xfc, 8, 0, ref i);
            AddCode(89, 0x73, 7, 0, ref i);
            AddCode(90, 0xfd, 8, 0, ref i);
            AddCode(91, 0x1ffb, 13, 0, ref i);
            AddCode(92, 0x7fff0, 19, 0, ref i);
            AddCode(93, 0x1ffc, 13, 0, ref i);
            AddCode(94, 0x3ffc, 14, 0, ref i);
            AddCode(95, 0x22, 6, 0, ref i);
            AddCode(96, 0x7ffd, 15, 0, ref i);
            AddCode(97, 0x3, 5, 0, ref i);
            AddCode(98, 0x23, 6, 0, ref i);
            AddCode(99, 0x4, 5, 0, ref i);
            AddCode(100, 0x24, 6, 0, ref i);
            AddCode(101, 0x5, 5, 0, ref i);
            AddCode(102, 0x25, 6, 0, ref i);
            AddCode(103, 0x26, 6, 0, ref i);
            AddCode(104, 0x27, 6, 0, ref i);
            AddCode(105, 0x6, 5, 0, ref i);
            AddCode(106, 0x74, 7, 0, ref i);
            AddCode(107, 0x75, 7, 0, ref i);
            AddCode(108, 0x28, 6, 0, ref i);
            AddCode(109, 0x29, 6, 0, ref i);
            AddCode(110, 0x2a, 6, 0, ref i);
            AddCode(111, 0x7, 5, 0, ref i);
            AddCode(112, 0x2b, 6, 0, ref i);
            AddCode(113, 0x76, 7, 0, ref i);
            AddCode(114, 0x2c, 6, 0, ref i);
            AddCode(115, 0x8, 5, 0, ref i);
            AddCode(116, 0x9, 5, 0, ref i);
            AddCode(117, 0x2d, 6, 0, ref i);
            AddCode(118, 0x77, 7, 0, ref i);
            AddCode(119, 0x78, 7, 0, ref i);
            AddCode(120, 0x79, 7, 0, ref i);
            AddCode(121, 0x7a, 7, 0, ref i);
            AddCode(122, 0x7b, 7, 0, ref i);
            AddCode(123, 0x7ffe, 15, 0, ref i);
            AddCode(124, 0x7fc, 11, 0, ref i);
            AddCode(125, 0x3ffd, 14, 0, ref i);
            AddCode(126, 0x1ffd, 13, 0, ref i);
            AddCode(127, 0xffffffc, 28, 0, ref i);
            AddCode(128, 0xfffe6, 20, 0, ref i);
            AddCode(129, 0x3fffd2, 22, 0, ref i);
            AddCode(130, 0xfffe7, 20, 0, ref i);
            AddCode(131, 0xfffe8, 20, 0, ref i);
            AddCode(132, 0x3fffd3, 22, 0, ref i);
            AddCode(133, 0x3fffd4, 22, 0, ref i);
            AddCode(134, 0x3fffd5, 22, 0, ref i);
            AddCode(135, 0x7fffd9, 23, 0, ref i);
            AddCode(136, 0x3fffd6, 22, 0, ref i);
            AddCode(137, 0x7fffda, 23, 0, ref i);
            AddCode(138, 0x7fffdb, 23, 0, ref i);
            AddCode(139, 0x7fffdc, 23, 0, ref i);
            AddCode(140, 0x7fffdd, 23, 0, ref i);
            AddCode(141, 0x7fffde, 23, 0, ref i);
            AddCode(142, 0xffffeb, 24, 0, ref i);
            AddCode(143, 0x7fffdf, 23, 0, ref i);
            AddCode(144, 0xffffec, 24, 0, ref i);
            AddCode(145, 0xffffed, 24, 0, ref i);
            AddCode(146, 0x3fffd7, 22, 0, ref i);
            AddCode(147, 0x7fffe0, 23, 0, ref i);
            AddCode(148, 0xffffee, 24, 0, ref i);
            AddCode(149, 0x7fffe1, 23, 0, ref i);
            AddCode(150, 0x7fffe2, 23, 0, ref i);
            AddCode(151, 0x7fffe3, 23, 0, ref i);
            AddCode(152, 0x7fffe4, 23, 0, ref i);
            AddCode(153, 0x1fffdc, 21, 0, ref i);
            AddCode(154, 0x3fffd8, 22, 0, ref i);
            AddCode(155, 0x7fffe5, 23, 0, ref i);
            AddCode(156, 0x3fffd9, 22, 0, ref i);
            AddCode(157, 0x7fffe6, 23, 0, ref i);
            AddCode(158, 0x7fffe7, 23, 0, ref i);
            AddCode(159, 0xffffef, 24, 0, ref i);
            AddCode(160, 0x3fffda, 22, 0, ref i);
            AddCode(161, 0x1fffdd, 21, 0, ref i);
            AddCode(162, 0xfffe9, 20, 0, ref i);
            AddCode(163, 0x3fffdb, 22, 0, ref i);
            AddCode(164, 0x3fffdc, 22, 0, ref i);
            AddCode(165, 0x7fffe8, 23, 0, ref i);
            AddCode(166, 0x7fffe9, 23, 0, ref i);
            AddCode(167, 0x1fffde, 21, 0, ref i);
            AddCode(168, 0x7fffea, 23, 0, ref i);
            AddCode(169, 0x3fffdd, 22, 0, ref i);
            AddCode(170, 0x3fffde, 22, 0, ref i);
            AddCode(171, 0xfffff0, 24, 0, ref i);
            AddCode(172, 0x1fffdf, 21, 0, ref i);
            AddCode(173, 0x3fffdf, 22, 0, ref i);
            AddCode(174, 0x7fffeb, 23, 0, ref i);
            AddCode(175, 0x7fffec, 23, 0, ref i);
            AddCode(176, 0x1fffe0, 21, 0, ref i);
            AddCode(177, 0x1fffe1, 21, 0, ref i);
            AddCode(178, 0x3fffe0, 22, 0, ref i);
            AddCode(179, 0x1fffe2, 21, 0, ref i);
            AddCode(180, 0x7fffed, 23, 0, ref i);
            AddCode(181, 0x3fffe1, 22, 0, ref i);
            AddCode(182, 0x7fffee, 23, 0, ref i);
            AddCode(183, 0x7fffef, 23, 0, ref i);
            AddCode(184, 0xfffea, 20, 0, ref i);
            AddCode(185, 0x3fffe2, 22, 0, ref i);
            AddCode(186, 0x3fffe3, 22, 0, ref i);
            AddCode(187, 0x3fffe4, 22, 0, ref i);
            AddCode(188, 0x7ffff0, 23, 0, ref i);
            AddCode(189, 0x3fffe5, 22, 0, ref i);
            AddCode(190, 0x3fffe6, 22, 0, ref i);
            AddCode(191, 0x7ffff1, 23, 0, ref i);
            AddCode(192, 0x3ffffe0, 26, 0, ref i);
            AddCode(193, 0x3ffffe1, 26, 0, ref i);
            AddCode(194, 0xfffeb, 20, 0, ref i);
            AddCode(195, 0x7fff1, 19, 0, ref i);
            AddCode(196, 0x3fffe7, 22, 0, ref i);
            AddCode(197, 0x7ffff2, 23, 0, ref i);
            AddCode(198, 0x3fffe8, 22, 0, ref i);
            AddCode(199, 0x1ffffec, 25, 0, ref i);
            AddCode(200, 0x3ffffe2, 26, 0, ref i);
            AddCode(201, 0x3ffffe3, 26, 0, ref i);
            AddCode(202, 0x3ffffe4, 26, 0, ref i);
            AddCode(203, 0x7ffffde, 27, 0, ref i);
            AddCode(204, 0x7ffffdf, 27, 0, ref i);
            AddCode(205, 0x3ffffe5, 26, 0, ref i);
            AddCode(206, 0xfffff1, 24, 0, ref i);
            AddCode(207, 0x1ffffed, 25, 0, ref i);
            AddCode(208, 0x7fff2, 19, 0, ref i);
            AddCode(209, 0x1fffe3, 21, 0, ref i);
            AddCode(210, 0x3ffffe6, 26, 0, ref i);
            AddCode(211, 0x7ffffe0, 27, 0, ref i);
            AddCode(212, 0x7ffffe1, 27, 0, ref i);
            AddCode(213, 0x3ffffe7, 26, 0, ref i);
            AddCode(214, 0x7ffffe2, 27, 0, ref i);
            AddCode(215, 0xfffff2, 24, 0, ref i);
            AddCode(216, 0x1fffe4, 21, 0, ref i);
            AddCode(217, 0x1fffe5, 21, 0, ref i);
            AddCode(218, 0x3ffffe8, 26, 0, ref i);
            AddCode(219, 0x3ffffe9, 26, 0, ref i);
            AddCode(220, 0xffffffd, 28, 0, ref i);
            AddCode(221, 0x7ffffe3, 27, 0, ref i);
            AddCode(222, 0x7ffffe4, 27, 0, ref i);
            AddCode(223, 0x7ffffe5, 27, 0, ref i);
            AddCode(224, 0xfffec, 20, 0, ref i);
            AddCode(225, 0xfffff3, 24, 0, ref i);
            AddCode(226, 0xfffed, 20, 0, ref i);
            AddCode(227, 0x1fffe6, 21, 0, ref i);
            AddCode(228, 0x3fffe9, 22, 0, ref i);
            AddCode(229, 0x1fffe7, 21, 0, ref i);
            AddCode(230, 0x1fffe8, 21, 0, ref i);
            AddCode(231, 0x7ffff3, 23, 0, ref i);
            AddCode(232, 0x3fffea, 22, 0, ref i);
            AddCode(233, 0x3fffeb, 22, 0, ref i);
            AddCode(234, 0x1ffffee, 25, 0, ref i);
            AddCode(235, 0x1ffffef, 25, 0, ref i);
            AddCode(236, 0xfffff4, 24, 0, ref i);
            AddCode(237, 0xfffff5, 24, 0, ref i);
            AddCode(238, 0x3ffffea, 26, 0, ref i);
            AddCode(239, 0x7ffff4, 23, 0, ref i);
            AddCode(240, 0x3ffffeb, 26, 0, ref i);
            AddCode(241, 0x7ffffe6, 27, 0, ref i);
            AddCode(242, 0x3ffffec, 26, 0, ref i);
            AddCode(243, 0x3ffffed, 26, 0, ref i);
            AddCode(244, 0x7ffffe7, 27, 0, ref i);
            AddCode(245, 0x7ffffe8, 27, 0, ref i);
            AddCode(246, 0x7ffffe9, 27, 0, ref i);
            AddCode(247, 0x7ffffea, 27, 0, ref i);
            AddCode(248, 0x7ffffeb, 27, 0, ref i);
            AddCode(249, 0xffffffe, 28, 0, ref i);
            AddCode(250, 0x7ffffec, 27, 0, ref i);
            AddCode(251, 0x7ffffed, 27, 0, ref i);
            AddCode(252, 0x7ffffee, 27, 0, ref i);
            AddCode(253, 0x7ffffef, 27, 0, ref i);
            AddCode(254, 0x7fffff0, 27, 0, ref i);
            AddCode(255, 0x3ffffee, 26, 0, ref i);
            AddCode(256, 0x3fffffff, 30, 0, ref i);
        }

        private static void AddCode(int c, int bits, int length, int currentState, ref int nextFreeState)
        {
            ref var state = ref states[currentState];
            if (length == 0)
            {
                Debug.Assert(!state.EndPoint);
                Debug.Assert(state.Left == 0);
                Debug.Assert(state.Right == 0);

                state.EndPoint = true;
                state.Left = state.Right = c;
            }
            else
            {
                var currentBit = (bits & (1 << (length - 1))) != 0;
                Debug.Assert(!state.EndPoint);
                if (currentBit)
                {
                    if (state.Right == 0)
                    {
                        state.Right = nextFreeState++;
                    }

                    AddCode(c, bits, length - 1, state.Right, ref nextFreeState);
                }
                else
                {
                    if (state.Left == 0)
                    {
                        state.Left = nextFreeState++;
                    }

                    AddCode(c, bits, length - 1, state.Left, ref nextFreeState);
                }
            }
        }

        public static string ReadString(ReadOnlySpan<byte> span)
        {
            var tmp = new byte[span.Length * 4];
            var length = 0;
            var currentState = states[0];
            foreach (var value in span)
            {
                for (var j = 7; j >= 0; j--)
                {
                    var bit = (value & (1 << j)) != 0;
                    currentState = states[bit ? currentState.Right : currentState.Left];
                    if (currentState.EndPoint)
                    {
                        if (currentState.Left == 0x100)
                        {
                            throw new NotImplementedException();
                        }

                        tmp[length++] = (byte)currentState.Left;
                        currentState = states[0];
                    }
                }
            }
            // TODO: Check that it all ends in 1 bits and less then 8 bits long
            return Encoding.UTF8.GetString(tmp, 0, length);
        }
    }
}