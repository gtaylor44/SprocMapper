﻿namespace SprocMapperLibrary.Core
{
    public static class PropertyMapper
    {
        public static MapObject<T> MapObject<T>()
        {
            return new MapObject<T>();
        }
    }
}