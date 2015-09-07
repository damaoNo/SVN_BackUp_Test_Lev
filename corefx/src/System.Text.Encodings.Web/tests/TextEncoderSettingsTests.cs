﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public static class TextEncoderSettingsExtensions
    {
        public static bool IsCharacterAllowed(this TextEncoderSettings settings, char character)
        {
            var bitmap = settings.GetAllowedCharacters();
            return bitmap.IsCharacterAllowed(character);
        }
    }

    public class TextEncoderSettingsTests
    {
        [Fact]
        public void Ctor_Parameterless_CreatesEmptyFilter()
        {
            var filter = new TextEncoderSettings();
            Assert.Equal(0, filter.GetAllowedCodePoints().Count());
        }

        [Fact]
        public void Ctor_OtherTextEncoderSettingsAsInterface()
        {
            // Arrange
            var originalFilter = new OddTextEncoderSettings();

            // Act
            var newFilter = new TextEncoderSettings(originalFilter);

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.Equal((i % 2) == 1, newFilter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Ctor_OtherTextEncoderSettingsAsConcreteType_Clones()
        {
            // Arrange
            var originalFilter = new TextEncoderSettings();
            originalFilter.AllowCharacter('x');

            // Act
            var newFilter = new TextEncoderSettings(originalFilter);
            newFilter.AllowCharacter('y');

            // Assert
            Assert.True(originalFilter.IsCharacterAllowed('x'));
            Assert.False(originalFilter.IsCharacterAllowed('y'));
            Assert.True(newFilter.IsCharacterAllowed('x'));
            Assert.True(newFilter.IsCharacterAllowed('y'));
        }

        [Fact]
        public void Ctor_UnicodeRanges()
        {
            // Act
            var filter = new TextEncoderSettings(UnicodeRanges.LatinExtendedA, UnicodeRanges.LatinExtendedC);

            // Assert
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i < 0x2C60; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C60; i <= 0x2C7F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C80; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowChar()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowCharacter('\u0100');

            // Assert
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
        }

        [Fact]
        public void AllowChars_Array()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowCharacters('\u0100', '\u0102');

            // Assert
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
            Assert.True(filter.IsCharacterAllowed('\u0102'));
            Assert.False(filter.IsCharacterAllowed('\u0103'));
        }

        [Fact]
        public void AllowChars_String()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowCharacters('\u0100', '\u0102');

            // Assert
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
            Assert.True(filter.IsCharacterAllowed('\u0102'));
            Assert.False(filter.IsCharacterAllowed('\u0103'));
        }

        [Fact]
        public void AllowFilter()
        {
            // Arrange
            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin);
            filter.AllowCodePoints(new OddTextEncoderSettings().GetAllowedCodePoints());

            // Assert
            for (int i = 0; i <= 0x007F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0080; i <= Char.MaxValue; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowRange()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowRange(UnicodeRanges.LatinExtendedA);

            // Assert
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowRanges()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowRanges(UnicodeRanges.LatinExtendedA, UnicodeRanges.LatinExtendedC);

            // Assert
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i < 0x2C60; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C60; i <= 0x2C7F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C80; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Clear()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            for (int i = 1; i <= Char.MaxValue; i++)
            {
                filter.AllowCharacter((char)i);
            }

            // Act
            filter.Clear();

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void ForbidChar()
        {
            // Arrange
            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin);
            filter.ForbidCharacter('x');

            // Assert
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.True(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidChars_Array()
        {
            // Arrange
            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin);
            filter.ForbidCharacters('x', 'z');

            // Assert
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.False(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidChars_String()
        {
            // Arrange
            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin);
            filter.ForbidCharacters('x', 'z');

            // Assert
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.False(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidRange()
        {
            // Arrange
            var filter = new TextEncoderSettings(new OddTextEncoderSettings());
            filter.ForbidRange(UnicodeRanges.Specials);

            // Assert
            for (int i = 0; i <= 0xFFEF; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0xFFF0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void ForbidRanges()
        {
            // Arrange
            var filter = new TextEncoderSettings(new OddTextEncoderSettings());
            filter.ForbidRanges(UnicodeRanges.BasicLatin, UnicodeRanges.Specials);

            // Assert
            for (int i = 0; i <= 0x007F; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0080; i <= 0xFFEF; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0xFFF0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void GetAllowedCodePoints()
        {
            // Arrange
            var expected = Enumerable.Range(UnicodeRanges.BasicLatin.FirstCodePoint, UnicodeRanges.BasicLatin.Length)
                .Concat(Enumerable.Range(UnicodeRanges.Specials.FirstCodePoint, UnicodeRanges.Specials.Length))
                .Except(new int[] { 'x' })
                .OrderBy(i => i)
                .ToArray();

            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Specials);
            filter.ForbidCharacter('x');

            // Act
            var retVal = filter.GetAllowedCodePoints().OrderBy(i => i).ToArray();

            // Assert
            Assert.Equal<int>(expected, retVal);
        }

        // a code point filter which allows only odd code points through
        private sealed class OddTextEncoderSettings : TextEncoderSettings
        {
            public override IEnumerable<int> GetAllowedCodePoints()
            {
                for (int i = 1; i <= Char.MaxValue; i += 2)
                {
                    yield return i;
                }
            }
        }
    }
}
