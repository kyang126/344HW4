using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
   public class BuildTrie
    {
        private node root;

        //Constructor: initilizes the trie by creating the root node
        public BuildTrie()
        {
            this.root = new node();
        }

        //Method adds each word from the wiki file into the trie
        public void addWord(String term)
        {
            node current = this.root;
            //Standardizes the words in the file
            String word = term.ToLower().Replace('_', ' ');

            //Traverses each character in the term and adds it to a node if the child node wasn't already created
            for (int i = 0; i < word.Length; i++)
            {
                if (!current.getChildren().ContainsKey(word[i]))
                {
                    node temp = new node();
                    current.addChild(word[i], temp);
                }
                current = current.getChildren()[word[i]];
            }

            //At the end of the word, I set the node where that character landed on as true for where the word ended
            current.setWord();
        }

        //Takes the input from a text field and searches the trie. Outputs a list of up to 10 words that closely matches the input
        public List<String> search(String term)
        {
            String word = term.ToLower();
            node current = this.root;

            //This list holds suggestion words
            List<String> wordBank = new List<String>();

            //Checks if the string is empty and if it is return empty output
            if (word.Length == 0)
            {
                return wordBank;
            }

            //traverses the trie up to the point where the term ends
            for (int i = 0; i < word.Length; i++)
            {
                if (!current.getChildren().ContainsKey(word[i]))
                {
                    return wordBank;
                }
                current = current.getChildren()[word[i]];
            }

            //if the term itself is a word than it will be added to the list
            if (current.isWord())
            {
                wordBank.Add(word);
            }

            //Searches the rest of this branch to add the words to the list and returns it
            return GetAutoCompleteList(current, word, wordBank);
        }

        //Takes the node that the term ended on, the term string, and the word list and returns the completed word list
        public List<String> GetAutoCompleteList(node matchedNode, string completeWord, List<String> wordBank)
        {

            //Returns the list once there is 10 words
            if (wordBank.Count >= 10)
            {
                return wordBank;
            }

            //Traverses the trie completely for the branch, adding the word once it hits the node that tells it is the word
            foreach (var key in matchedNode.getChildren().Keys)
            {
                string tmpWord = completeWord + key;
                var value = matchedNode.getChildren()[key];
                if (value.isWord() && wordBank.Count < 10)
                {
                    wordBank.Add(tmpWord);
                }
                GetAutoCompleteList(value, tmpWord, wordBank);
            }
            return wordBank;
        }
    }
}
