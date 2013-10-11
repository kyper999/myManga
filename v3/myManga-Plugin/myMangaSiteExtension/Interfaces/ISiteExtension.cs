﻿using System;
using System.Collections.Generic;
using System.IO;
using Core.IO;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension
    {
        String GetSearchUri(String searchTerm);

        MangaObject ParseMangaObject(String content);
        ChapterObject ParseChapterObject(String content);
        PageObject ParsePageObject(String content);
        List<SearchResultObject> ParseSearch(String content);
    }
}
