import pygame as pg
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter import ttk, scrolledtext
from pathlib import Path
import io
import zipfile

pg.init()

WHITE = (255, 255, 255)
TRANSPARENT = (0, 0, 0, 0)
BLACK = (0, 0, 0)

def y_cut(surface, background_color=None):
    width, height = surface.get_size()
    top = height
    bottom = 0
    for y in range(height):
        for x in range(width):
            pixel = surface.get_at((x, y))
            if background_color is not None:
                is_content = pixel[:3] != background_color[:3]
            else:
                is_content = pixel.a > 0
            if is_content:
                top = min(top, y)
                bottom = max(bottom, y)
    if bottom < top:
        return pg.Surface((0, 0), pg.SRCALPHA), 0, 0
    crop_height = bottom - top + 1
    cropped = pg.Surface((width, crop_height), pg.SRCALPHA)
    cropped.blit(surface, (0, 0), (0, top, width, crop_height))
    return cropped, top, bottom

def x_cut(surface, background_color=None):
    width, height = surface.get_size()
    left = width
    right = 0
    for y in range(height):
        for x in range(width):
            pixel = surface.get_at((x, y))
            if background_color is not None:
                is_content = pixel[:3] != background_color[:3]
            else:
                is_content = pixel.a > 0
            if is_content:
                left = min(left, x)
                right = max(right, x)
    if right < left:
        return pg.Surface((0, 0), pg.SRCALPHA)
    crop_width = right - left + 1
    cropped = pg.Surface((crop_width, height), pg.SRCALPHA)
    cropped.blit(surface, (0, 0), (left, 0, crop_width, height))
    return cropped

def get_text_surface(font, text, color):
    return font.render(text, True, color)

class SpriteFontPackerUI:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("SpriteFont Packer")
        self.root.geometry("600x450")
        self.font_path = None
        
        ttk.Label(self.root, text="Characters to include:", font=("Arial", 12)).pack(pady=10)
        
        self.text_box = scrolledtext.ScrolledText(self.root, width=70, height=15, wrap=tk.WORD, font=("Arial", 10), undo=True)
        self.text_box.pack(pady=10, padx=20)
        default_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"
        self.text_box.insert("1.0", default_chars)
        
        self.font_label = ttk.Label(self.root, text="No font selected", font=("Arial", 10), foreground="gray")
        self.font_label.pack(pady=5)
        
        button_frame = ttk.Frame(self.root)
        button_frame.pack(pady=10)
        
        self.choose_font_btn = ttk.Button(button_frame, text="Choose TTF Font", command=self.choose_font, width=20)
        self.choose_font_btn.pack(side=tk.LEFT, padx=10)
        
        self.export_btn = ttk.Button(button_frame, text="Export SFPF", command=self.export_sfpf, width=20, state=tk.DISABLED)
        self.export_btn.pack(side=tk.LEFT, padx=10)
        
        self.status_label = ttk.Label(self.root, text="", font=("Arial", 9), foreground="black", wraplength=550)
        self.status_label.pack(pady=5)
        
    def choose_font(self):
        font_path = filedialog.askopenfilename(
            title="Select Font",
            filetypes=[("TrueType font", "*.ttf")]
        )
        if font_path:
            self.font_path = font_path
            self.font_label.config(text=f"Font: {Path(font_path).name}", foreground="green")
            self.export_btn.config(state=tk.NORMAL)
    
    def export_sfpf(self):
        if not self.font_path:
            messagebox.showerror("Error", "Please choose a font first!")
            return
        
        chars_input = self.text_box.get("1.0", tk.END).strip()
        if not chars_input:
            messagebox.showerror("Error", "Please enter characters to include!")
            return
        
        self.status_label.config(text="Processing...", foreground="orange")
        self.root.update()
        
        try:
            font = pg.Font(self.font_path, 120)
            chars_to_use = chars_input.replace(" ", "").replace("\n", "")
            chars = list(chars_to_use + ("" if "?" in chars_to_use else "?"))
            chars_surfaces = dict()
            
            for c in chars:
                sf = get_text_surface(font, c, WHITE)
                chars_surfaces[c] = sf
            
            min_y = 6969696969
            max_y = 0
            for c, sf in chars_surfaces.items():
                cropped, top, bottom = y_cut(sf)
                if cropped.height == 0:
                    chars.remove(c)
                    continue
                min_y = min(min_y, top)
                max_y = max(max_y, bottom)
            
            cropped_chars_surfaces = dict()
            for c, sf in chars_surfaces.items():
                cropped = x_cut(sf)
                cropped = cropped.subsurface((0, min_y, cropped.width, max_y - min_y)).copy()
                cropped_chars_surfaces[c] = cropped
            
            w = 0
            chars_text = "".join(chars)
            for c in chars_text:
                if c not in chars:
                    continue
                w += cropped_chars_surfaces[c].width
            
            atlas = pg.surface.Surface((w, max_y - min_y), pg.SRCALPHA)
            atlas.fill(TRANSPARENT)
            chars_data = ""
            curr_x = 0
            
            for c in chars_text:
                if c not in chars:
                    continue
                sf = cropped_chars_surfaces[c]
                atlas.blit(sf, (curr_x, 0))
                chars_data += f"{c} {curr_x} {sf.width}\n"
                curr_x += sf.width
            
            chars_data = chars_data[:-1]
            
            self.status_label.config(text="Done Processing", foreground="green")
            self.root.update()
            
            save_path = filedialog.asksaveasfilename(
                title="Save SpriteFont Proof Font",
                initialfile=f"{Path(self.font_path).stem}.sfpf",
                defaultextension=".sfpf",
                filetypes=[("SpriteFont Proof Font", "*.sfpf")]
            )
            
            if save_path:
                font_bytes = io.BytesIO()
                pg.image.save(atlas, font_bytes, 'PNG')
                with zipfile.ZipFile(save_path, 'w') as z:
                    z.writestr('atlas', font_bytes.getvalue())
                    z.writestr('chars_data', chars_data)
                
                self.status_label.config(text=f"Exported successfully!", foreground="green")
                messagebox.showinfo("Success", f"Font exported to:\n{save_path}")
            else:
                self.status_label.config(text="", foreground="black")
        
        except Exception as e:
            self.status_label.config(text="Export failed!", foreground="red")
            messagebox.showerror("Error", f"Export failed:\n{str(e)}")
    
    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    app = SpriteFontPackerUI()
    app.run()
