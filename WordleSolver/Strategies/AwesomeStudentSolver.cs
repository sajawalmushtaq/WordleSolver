
using System.Collections.Generic;

namespace WordleSolver.Strategies;
public sealed class AwesomeStudentSolver : IWordleSolverStrategy
{
	/// <summary>Absolute or relative path of the word-list file.</summary>
	private static readonly string WordListPath = Path.Combine("data", "wordle.txt");

	/// <summary>In-memory dictionary of valid five-letter words.</summary>
	private static readonly List<string> WordList = LoadWordList();

    /// <summary>
    /// Remaining words that can be chosen
    /// </summary>
    private List<string> _remainingWords = new();
    
    // TODO: ADD your own private variables that you might need

    /// <summary>
    /// Loads the dictionary from disk, filtering to distinct five-letter lowercase words.
    /// </summary>
    private static List<string> LoadWordList()
    {
	    if (!File.Exists(WordListPath))
		    throw new FileNotFoundException($"Word list not found at path: {WordListPath}");

	    return File.ReadAllLines(WordListPath)
		    .Select(w => w.Trim().ToLowerInvariant())
		    .Where(w => w.Length == 5)
		    .Distinct()
		    .ToList();
    }

    /// <inheritdoc/>
    public void Reset()
    {
		// TODO: What should happen when a new game starts?

		// If using SLOW student strategy, we just reset the current index
		// to the first word to start the next guessing sequence
        _remainingWords = [..WordList];  // Set _remainingWords to a copy of the full word list
    }

    /// <summary>
    /// Determines the next word to guess given feedback from the previous guess.
    /// </summary>
    /// <param name="previousResult">
    /// The <see cref="GuessResult"/> returned by the game engine for the last guess
    /// (or <see cref="GuessResult.Default"/> if this is the first turn).
    /// </param>
    /// <returns>A five-letter lowercase word.</returns>
    public string PickNextGuess(GuessResult previousResult)
    {
        if (!previousResult.IsValid)
            throw new InvalidOperationException("PickNextGuess shouldn't be called if previous result isn't valid");

        if (previousResult.Guesses.Count == 0)
        {
            string firstWord = "canoe";
            _remainingWords.Remove(firstWord);
            return firstWord;
        }

        string lastGuess = previousResult.Word;
        LetterStatus[] statuses = previousResult.LetterStatuses;

        var requiredLetters = new Dictionary<char, int>();
        var misplacedLetters = new List<(char letter, int index)>();
        var correctLetters = new Dictionary<int, char>();
        var forbiddenPositions = new Dictionary<char, HashSet<int>>();
        var globallyBannedLetters = new HashSet<char>();

        // Analyze the last result
        for (int i = 0; i < 5; i++)
        {
            char c = lastGuess[i];
            var status = statuses[i];

            switch (status)
            {
                case LetterStatus.Unused:
                    // Check if this letter was marked as used elsewhere (misplaced/correct)
                    bool usedElsewhere = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (j != i && lastGuess[j] == c && statuses[j] != LetterStatus.Unused)
                        {
                            usedElsewhere = true;
                            break;
                        }
                    }

                    if (usedElsewhere)
                    {
                        if (!forbiddenPositions.ContainsKey(c))
                            forbiddenPositions[c] = new();
                        forbiddenPositions[c].Add(i);
                    }
                    else
                    {
                        globallyBannedLetters.Add(c);
                    }
                    break;

                case LetterStatus.Misplaced:
                    misplacedLetters.Add((c, i));
                    if (!requiredLetters.ContainsKey(c))
                        requiredLetters[c] = 0;
                    requiredLetters[c]++;
                    if (!forbiddenPositions.ContainsKey(c))
                        forbiddenPositions[c] = new();
                    forbiddenPositions[c].Add(i);
                    break;

                case LetterStatus.Correct:
                    correctLetters[i] = c;
                    if (!requiredLetters.ContainsKey(c))
                        requiredLetters[c] = 0;
                    requiredLetters[c]++;
                    break;
            }
        }

        // Apply filters
        _remainingWords = _remainingWords
            .Where(word =>
            {
                // Filter out words with globally banned letters
                if (globallyBannedLetters.Any(b => word.Contains(b)))
                    return false;

                // Correct letters at positions
                foreach (var (index, ch) in correctLetters)
                    if (word[index] != ch) return false;

                // Misplaced: must have the letter, but not at that index
                foreach (var (ch, idx) in misplacedLetters)
                    if (word[idx] == ch || !word.Contains(ch)) return false;

                // Forbidden positions
                foreach (var (ch, positions) in forbiddenPositions)
                    foreach (var pos in positions)
                        if (word[pos] == ch) return false;

                // Required letters (min frequency check)
                foreach (var (ch, minCount) in requiredLetters)
                    if (word.Count(c => c == ch) < minCount) return false;

                return true;
            })
            .ToList();

        string choice = ChooseBestRemainingWord(previousResult);
        _remainingWords.Remove(choice);
        return choice;
    }


    /// <summary>
    /// Pick the best of the remaining words according to some heuristic.
    /// For example, you might want to choose the word that has the most
    /// common letters found in the remaining words list
    /// </summary>
    /// <param name="previousResult"></param>
    /// <returns></returns>
    public string ChooseBestRemainingWord(GuessResult previousResult)
    {
        if (_remainingWords.Count == 0)
            throw new InvalidOperationException("No remaining words to choose from");

        // Obviously the first word is the best right?
        return _remainingWords.First();  
    }
}