namespace MagicConchBot.Helpers
{
    using System;
    using System.Diagnostics.Contracts;

    public static class AudioHelper
    {
        public static unsafe byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            Contract.Requires(audioSamples != null);
            Contract.Requires(audioSamples.Length % 2 == 0);
            Contract.Requires(volume >= 0f && volume <= 1f);
            Contract.Assert(BitConverter.IsLittleEndian);

            if (Math.Abs(volume - 1f) < 0.0001f)
            {
                return audioSamples;
            }

            // 16-bit precision for the multiplication
            var volumeFixed = (int)Math.Round(volume * 65536d);

            var count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*)srcBytes;

                for (var i = count; i != 0; i--, src++)
                {
                    *src = (short)(((*src) * volumeFixed) >> 16);
                }
            }

            return audioSamples;
        }
    }
}
