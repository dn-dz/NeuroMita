### main.py

from gui import ChatGUI


# Установка

#"""
#Тестово, потом надо будет вот это вернуть
#pyinstaller --name NeuroMita --noconfirm --add-data "Prompts/*;Prompts" --add-data "%USERPROFILE%\AppData\Local\Programs\Python\Python313\Lib\site-packages\emoji\unicode_codes\emoji.json;emoji\unicode_codes  --add-data "Prompts/**/*;Prompts" Main.py
#"""


# Теперь делаю файлом с папкой, так как антивирусы ругаются)
#pyinstaller --name NeuroMita --noconfirm --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Старый вариант
#pyinstaller --onefile --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Не забудь рядом папку промптов и ffmpeg

def main():
    gui = ChatGUI()
    gui.run()


if __name__ == "__main__":
    main()
