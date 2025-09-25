/**
 * @license Copyright (c) 2014-2023, CKSource Holding sp. z o.o. All rights reserved.
 * For licensing, see LICENSE.md or https://ckeditor.com/legal/ckeditor-oss-license
 */
import { ClassicEditor } from '@ckeditor/ckeditor5-editor-classic';
import type { EditorConfig } from '@ckeditor/ckeditor5-core';
import MathType from '@wiris/mathtype-ckeditor5/src/plugin';
declare class Editor extends ClassicEditor {
    static builtinPlugins: (typeof MathType)[];
    static defaultConfig: EditorConfig;
}
export default Editor;
