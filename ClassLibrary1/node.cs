using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace ClassLibrary1
{
    public class node
    {
        private SortedDictionary<Char, node> children;
        private Boolean complete;
        private List<String> wordList;
        //Constructor: initializes the node by creating a dictionary for the children and a boolean value
        public node()
        {
            this.children = new SortedDictionary<Char, node>();
            this.complete = false;
            this.wordList = new List<String>();
        }

        //Returns the dictionary that holds the children
        public SortedDictionary<Char, node> getChildren()
        {
            return this.children;
        }

        //Returns the dictionary that holds the children
        public List<String> getWordList()
        {
            this.wordList.Sort();
            return this.wordList;
        }


        public void addToList(String a)
        {
            this.wordList.Add(a);
        }


        //Allows the trie class to add a child (node) to the children dictionary
        public void addChild(Char c, node n)
        {
            this.children.Add(c, n);
        }

        //Places a check on the node if the word is completed there
        public void setWord()
        {
            this.complete = true;
        }

        //Checks if the word is completed at this node
        public Boolean isWord()
        {
            return this.complete;
        }
    }
}
