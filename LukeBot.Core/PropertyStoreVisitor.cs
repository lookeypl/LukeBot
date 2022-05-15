namespace LukeBot.Core
{
    internal interface PropertyStoreVisitor
    {
        void Visit<T>(PropertyType<T> p);
        void VisitStart(PropertyDomain pd);
        void VisitEnd(PropertyDomain pd);
    }
}